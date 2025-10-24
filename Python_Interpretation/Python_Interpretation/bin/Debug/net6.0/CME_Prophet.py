from datetime import datetime
import requests
import pandas as pd
from prophet import Prophet
import xml.etree.ElementTree as ET
import base64

# API-ключ
api_key = "0j8sbNJdINmIyT1dPVmLTxCcPgXLhNxAAfPYahfk"

start_date = "2012-01-01"
end_date = datetime.today().strftime("%Y-%m-%d")

# Функция для получения данных из API с обработкой ошибок
def fetch_data(url):
    try:
        response = requests.get(url)
        response.raise_for_status()
        return response.json()
    except requests.exceptions.RequestException as e:
        return f"Ошибка запроса API ({url}): {e}"

# Функция для преобразования времени в объект datetime
def parse_date(date_str):
    try:
        return datetime.strptime(date_str, "%Y-%m-%dT%H:%MZ").date()
    except ValueError as e:
        return f"Ошибка парсинга даты ({date_str}): {e}"

# Обработка данных
status = "Failure"
try:
    # Получаем данные из API
    cme_url = f'https://api.nasa.gov/DONKI/CMEAnalysis?startDate={start_date}&endDate={end_date}&mostAccurateOnly=true&api_key={api_key}'
    gst_url = f'https://api.nasa.gov/DONKI/GST?startDate={start_date}&endDate={end_date}&api_key={api_key}'
    flr_url = f'https://api.nasa.gov/DONKI/FLR?startDate={start_date}&endDate={end_date}&api_key={api_key}'

    cme_data = fetch_data(cme_url)
    gst_data = fetch_data(gst_url)
    flr_data = fetch_data(flr_url)

    # Проверка ошибок при получении API-данных
    if isinstance(cme_data, str) or isinstance(gst_data, str) or isinstance(flr_data, str):
        raise Exception(f"Ошибка API: {cme_data} | {gst_data} | {flr_data}")

    processed_data = []

    for cme in cme_data:
        cme_time = parse_date(cme.get('time21_5'))
        if isinstance(cme_time, str):
            raise Exception(cme_time)

        cme_info = {
            'cme_time': cme['time21_5'],
            'speed': cme.get('speed', 0),
            'halfAngle': cme.get('halfAngle', 0),
            'latitude': cme.get('latitude'),
            'longitude': cme.get('longitude'),
            'storm_occurred': 'No',
            'flare_occurred': 'No',
        }

        # Проверка геомагнитных бурь (GST)
        for gst in gst_data:
            gst_time = parse_date(gst.get('startTime'))
            if isinstance(gst_time, str):
                raise Exception(gst_time)
            if abs((gst_time - cme_time).days) <= 2:
                cme_info['storm_occurred'] = 'Yes'
                cme_info['kpIndex'] = max((item.get('kpIndex', 0) for item in gst.get('allKpIndex', [])), default=None)
                break

        # Проверка солнечных вспышек (FLR)
        for flr in flr_data:
            flr_time = parse_date(flr.get('beginTime'))
            if isinstance(flr_time, str):
                raise Exception(flr_time)
            if abs((flr_time - cme_time).days) <= 2:
                cme_info['flare_occurred'] = 'Yes'
                cme_info['classType'] = flr.get('classType', 'Unknown')
                cme_info['sourceLocation'] = flr.get('sourceLocation', 'Unknown')
                break

        processed_data.append(cme_info)

    # Создаем DataFrame
    df = pd.DataFrame(processed_data)
    df['cme_time'] = pd.to_datetime(df['cme_time']).dt.date

    # Фильтрация бурь
    df_storms = df[df['storm_occurred'] == 'Yes']
    if df_storms.empty:
        raise ValueError("Недостаточно данных о бурях для обучения модели")

    # Подготовка данных для Prophet
    df_prophet = df_storms[['cme_time', 'kpIndex', 'speed', 'halfAngle']].rename(columns={'cme_time': 'ds', 'kpIndex': 'y'})
    df_prophet['ds'] = pd.to_datetime(df_prophet['ds'])

    # Проверка данных перед обучением
    if df_prophet.isnull().values.any():
        raise ValueError("Обнаружены пустые значения в данных для Prophet")

    # Создание и обучение модели Prophet
    model = Prophet()
    model.add_regressor('speed')
    model.add_regressor('halfAngle')
    model.fit(df_prophet)

    # Прогнозирование на 1 год вперед
    future = model.make_future_dataframe(periods=365)
    future['speed'] = df_prophet['speed'].mean()
    future['halfAngle'] = df_prophet['halfAngle'].mean()

    forecast = model.predict(future)

    # Преобразуем дату в строковый формат
    forecast['ds'] = pd.to_datetime(forecast['ds'])

    # Создание XML
    root = ET.Element("forecast_data")
    for _, row in forecast.iterrows():
        record = ET.SubElement(root, "record")
        ET.SubElement(record, "date").text = row['ds'].strftime('%Y-%m-%d')
        ET.SubElement(record, "value").text = str(row['yhat'])

    # Преобразование в строку
    xml_str = ET.tostring(root, encoding='utf-8', method='xml')
    encoded_xml = base64.b64encode(xml_str).decode('utf-8')
    xml_size = len(xml_str)

    # Успешный статус
    status = "Success"

except Exception as e:
    # В случае ошибки шифруем её в base64
    error_message = f"Ошибка: {str(e)}"
    xml_str = error_message.encode('utf-8')
    encoded_xml = base64.b64encode(xml_str).decode('utf-8')
    xml_size = len(xml_str)

# Вывод результата
print(f"{encoded_xml} {xml_size} {status}")

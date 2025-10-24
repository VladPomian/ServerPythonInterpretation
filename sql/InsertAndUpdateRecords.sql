USE [Storm_Prediction]
GO
/****** Object:  StoredProcedure [dbo].[InsertAndUpdateRecords]    Script Date: 24.10.2025 10:45:25 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[InsertAndUpdateRecords]
    @time_enter DATETIME,
    @data NVARCHAR(MAX),
    @size_data INT,
    @status NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRANSACTION;

    BEGIN TRY
        -- Шаг 1: Добавление записи в таблицу Archive
        INSERT INTO [dbo].[Archive] ([time_enter], [data], [size_data], [status])
        VALUES (@time_enter, @data, @size_data, @status);

        -- Шаг 2: Обновление последней строки в таблице Actual (независимо от статуса)
        UPDATE [dbo].[Actual]
        SET [time_enter] = @time_enter, [data] = @data, [size_data] = @size_data, [status] = @status

        -- Шаг 3: Если статус "Success", обновление последней строки в таблице LastSuccess
        IF @status = 'Success'
        BEGIN
            UPDATE [dbo].[LastSuccess]
            SET [time_enter] = @time_enter, [data] = @data, [size_data] = @size_data, [status] = @status;
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        PRINT ERROR_MESSAGE();
    END CATCH
END;

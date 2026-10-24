USE RestauranteControlDB;
GO

-- 1. Agregar la columna ImagenUrl a la tabla Producto
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Producto' AND COLUMN_NAME = 'ImagenUrl')
BEGIN
    ALTER TABLE dbo.Producto ADD ImagenUrl NVARCHAR(500) NULL;
END
GO

-- 2. Modificar el SP para recibir e insertar/actualizar la imagen
CREATE OR ALTER PROCEDURE dbo.sp_Producto_Guardar
    @Id int = NULL, 
    @Nombre nvarchar(150), 
    @Descripcion nvarchar(500) = NULL, 
    @CategoriaId int,
    @Precio decimal(18,2), 
    @Disponible bit = 1, 
    @Activo bit = 1, 
    @ImagenUrl nvarchar(500) = NULL,
    @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;
        SET @Nombre = LTRIM(RTRIM(ISNULL(@Nombre, N'')));
        SET @Descripcion = NULLIF(LTRIM(RTRIM(@Descripcion)), N'');
        IF @Nombre = N'' THROW 50401, N'El nombre del platillo es obligatorio.', 1;
        IF @Precio IS NULL OR @Precio < 0 THROW 50402, N'El precio no puede ser negativo.', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.CategoriaProducto WHERE Id = @CategoriaId) THROW 50403, N'La categoría indicada no existe.', 1;
        IF EXISTS (SELECT 1 FROM dbo.Producto WHERE Nombre = @Nombre AND CategoriaProductoId = @CategoriaId AND Id <> ISNULL(@Id, 0))
            THROW 50404, N'Ya existe un platillo con ese nombre en la categoría.', 1;

        IF @Id IS NULL
        BEGIN
            INSERT dbo.Producto(Nombre, Descripcion, CategoriaProductoId, Precio, Disponible, Activo, ImagenUrl)
            VALUES (@Nombre, @Descripcion, @CategoriaId, @Precio, @Disponible, @Activo, @ImagenUrl);
            SET @Id = SCOPE_IDENTITY();
            DECLARE @NuevoIns nvarchar(max) = CONCAT(N'Nombre=', @Nombre, N'; CategoriaId=', @CategoriaId, N'; Precio=', @Precio, N'; Disponible=', @Disponible, N'; Activo=', @Activo);
            EXEC dbo.sp_Bitacora_Registrar @UsuarioId, N'INSERT', N'Producto', @Id, NULL, @NuevoIns, N'Alta de platillo';
        END
        ELSE
        BEGIN
            DECLARE @NomA nvarchar(150), @CatA int, @PreA decimal(18,2), @DisA bit, @ActA bit;
            SELECT @NomA = Nombre, @CatA = CategoriaProductoId, @PreA = Precio, @DisA = Disponible, @ActA = Activo
            FROM dbo.Producto WITH (UPDLOCK) WHERE Id = @Id;
            IF @NomA IS NULL THROW 50405, N'El platillo no existe.', 1;

            UPDATE dbo.Producto
            SET Nombre = @Nombre, 
                Descripcion = @Descripcion, 
                CategoriaProductoId = @CategoriaId,
                Precio = @Precio, 
                Disponible = @Disponible, 
                Activo = @Activo,
                ImagenUrl = ISNULL(@ImagenUrl, ImagenUrl) -- Si no se sube foto nueva, mantiene la anterior
            WHERE Id = @Id;

            DECLARE @Ant nvarchar(max) = CONCAT(N'Nombre=', @NomA, N'; CategoriaId=', @CatA, N'; Precio=', @PreA, N'; Disponible=', @DisA, N'; Activo=', @ActA);
            DECLARE @Nue nvarchar(max) = CONCAT(N'Nombre=', @Nombre, N'; CategoriaId=', @CategoriaId, N'; Precio=', @Precio, N'; Disponible=', @Disponible, N'; Activo=', @Activo);
            EXEC dbo.sp_Bitacora_Registrar @UsuarioId, N'UPDATE', N'Producto', @Id, @Ant, @Nue, N'Edición de platillo';
        END;
        COMMIT;
        SELECT CAST(@Id AS bigint) AS Value;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO
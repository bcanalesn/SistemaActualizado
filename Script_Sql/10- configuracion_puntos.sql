CREATE TABLE IF NOT EXISTS puntos_configuracion (
    IdConfig INT NOT NULL PRIMARY KEY DEFAULT 1,
    Activo TINYINT(1) NOT NULL DEFAULT 1 COMMENT '1: Programa activo, 0: Desactivado',
    MontoBasePorPunto INT NOT NULL DEFAULT 1000 COMMENT 'Pesos gastados para acumular 1 punto',
    PuntosPorBloque INT NOT NULL DEFAULT 1 COMMENT 'Puntos entregados por cada bloque de monto base',
    MontoMinimoAcumulacion INT NOT NULL DEFAULT 1000 COMMENT 'Compra mínima para comenzar a acumular',
    ValorPesoPorPunto DECIMAL(10,2) NOT NULL DEFAULT 10.00 COMMENT 'Equivalencia de 1 punto en descuento en pesos',
    FechaModificacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UsuarioModifica VARCHAR(50) NOT NULL DEFAULT 'admin'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Registro inicial por defecto
INSERT INTO puntos_configuracion (IdConfig, Activo, MontoBasePorPunto, PuntosPorBloque, MontoMinimoAcumulacion, ValorPesoPorPunto, FechaModificacion, UsuarioModifica)
VALUES (1, 1, 1000, 1, 1000, 10.00, NOW(), 'admin')
ON DUPLICATE KEY UPDATE IdConfig = 1;
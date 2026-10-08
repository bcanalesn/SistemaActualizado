-- 1. Tabla de Zonas de Reparto
CREATE TABLE IF NOT EXISTS `zonas_delivery` (
    `ZonaDeliveryId` INT NOT NULL AUTO_INCREMENT,
    `Nombre` VARCHAR(100) NOT NULL,
    `CostoEnvio` DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    `PedidoMinimo` DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    `MontoEnvioGratis` DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    `Activo` TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (`ZonaDeliveryId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 2. Tabla de Comunas vinculadas a Zonas
CREATE TABLE IF NOT EXISTS `comunas_delivery` (
    `ComunaDeliveryId` INT NOT NULL AUTO_INCREMENT,
    `ZonaDeliveryId` INT NOT NULL,
    `Nombre` VARCHAR(120) NOT NULL,
    `TiempoEstimadoMinutos` INT NOT NULL DEFAULT 45,
    `Activo` TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (`ComunaDeliveryId`),
    KEY `IX_comunas_delivery_ZonaDeliveryId` (`ZonaDeliveryId`),
    CONSTRAINT `FK_comunas_delivery_zonas_delivery` FOREIGN KEY (`ZonaDeliveryId`) 
        REFERENCES `zonas_delivery` (`ZonaDeliveryId`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 3. Tabla Principal de Deliveries
CREATE TABLE IF NOT EXISTS `deliveries` (
    `DeliveryId` INT NOT NULL AUTO_INCREMENT,
    `NumeroPedidoDelivery` VARCHAR(20) NOT NULL,
    `VentaId` INT NULL,
    `ReservaId` INT NULL,
    `ZonaDeliveryId` INT NOT NULL,
    `ComunaDeliveryId` INT NOT NULL,
    `NombreZona` VARCHAR(100) NOT NULL,
    `NombreComuna` VARCHAR(100) NOT NULL,
    `NombreRecibe` VARCHAR(150) NOT NULL,
    `Telefono` VARCHAR(30) NOT NULL,
    `Direccion` VARCHAR(200) NOT NULL,
    `Numero` VARCHAR(20) NOT NULL,
    `Departamento` VARCHAR(50) NULL,
    `Referencia` VARCHAR(255) NULL,
    `Instrucciones` VARCHAR(255) NULL,
    `CostoDelivery` DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    `TotalVenta` DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    `TiempoEstimadoMinutos` INT NOT NULL DEFAULT 45,
    `EstadoDelivery` INT NOT NULL DEFAULT 1,
    `ObservacionEstado` VARCHAR(255) NULL,
    `FechaCreacion` DATETIME NOT NULL,
    `FechaPreparacion` DATETIME NULL,
    `FechaDespacho` DATETIME NULL,
    `FechaEntrega` DATETIME NULL,
    `Repartidor` VARCHAR(120) NULL,
    `Activo` TINYINT(1) NOT NULL DEFAULT 1,
    PRIMARY KEY (`DeliveryId`),
    UNIQUE KEY `UK_Delivery_NumeroPedido` (`NumeroPedidoDelivery`),
    KEY `IX_deliveries_VentaId` (`VentaId`),
    KEY `IX_deliveries_ReservaId` (`ReservaId`),
    KEY `IX_deliveries_ZonaDeliveryId` (`ZonaDeliveryId`),
    KEY `IX_deliveries_ComunaDeliveryId` (`ComunaDeliveryId`),
    CONSTRAINT `FK_deliveries_zonas` FOREIGN KEY (`ZonaDeliveryId`) REFERENCES `zonas_delivery` (`ZonaDeliveryId`),
    CONSTRAINT `FK_deliveries_comunas` FOREIGN KEY (`ComunaDeliveryId`) REFERENCES `comunas_delivery` (`ComunaDeliveryId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 4. Modificar tabla de ventas tve2607
ALTER TABLE `tve2607` 
    ADD COLUMN IF NOT EXISTS `TipoEntrega` INT NOT NULL DEFAULT 1,
    ADD COLUMN IF NOT EXISTS `CostoDelivery` DECIMAL(12,2) NOT NULL DEFAULT 0.00;

-- 5. Carga de Zonas iniciales
INSERT INTO `zonas_delivery` (`ZonaDeliveryId`, `Nombre`, `CostoEnvio`, `PedidoMinimo`, `MontoEnvioGratis`, `Activo`) VALUES
(1, 'Zona Centro', 2500.00, 8000.00, 30000.00, 1),
(2, 'Zona Oriente', 4000.00, 15000.00, 45000.00, 1),
(3, 'Zona Norte', 3500.00, 12000.00, 40000.00, 1),
(4, 'Zona Sur', 3500.00, 12000.00, 40000.00, 1),
(5, 'Zona Poniente', 4000.00, 15000.00, 45000.00, 1)
ON DUPLICATE KEY UPDATE `Nombre` = VALUES(`Nombre`);

-- 6. Carga de Comunas iniciales
INSERT INTO `comunas_delivery` (`ZonaDeliveryId`, `Nombre`, `TiempoEstimadoMinutos`, `Activo`) VALUES
(1, 'Santiago Centro', 35, 1),
(1, 'Estación Central', 40, 1),
(2, 'Providencia', 40, 1),
(2, 'Ñuñoa', 45, 1),
(2, 'Las Condes', 55, 1),
(2, 'La Reina', 50, 1),
(3, 'Recoleta', 40, 1),
(3, 'Independencia', 40, 1),
(3, 'Conchalí', 50, 1),
(4, 'San Miguel', 40, 1),
(4, 'San Joaquín', 45, 1),
(5, 'Quinta Normal', 45, 1),
(5, 'Maipú', 60, 1);
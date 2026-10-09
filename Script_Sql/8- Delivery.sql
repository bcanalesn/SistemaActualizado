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
-- Zona Centro (ID: 1) - 6 Comunas
(1, 'Santiago', 35, 1),
(1, 'San Miguel', 40, 1),
(1, 'San Joaquín', 40, 1),
(1, 'Providencia', 40, 1),
(1, 'Ñuñoa', 45, 1),
(1, 'Macul', 45, 1),

-- Zona Norte (ID: 3) - 6 Comunas
(3, 'Renca', 45, 1),
(3, 'Recoleta', 40, 1),
(3, 'Quilicura', 55, 1),
(3, 'Independencia', 40, 1),
(3, 'Huechuraba', 50, 1),
(3, 'Conchalí', 45, 1),

-- Zona Oriente (ID: 2) - 5 Comunas
(2, 'Vitacura', 50, 1),
(2, 'Lo Barnechea', 60, 1),
(2, 'Las Condes', 50, 1),
(2, 'La Reina', 45, 1),
(2, 'Peñalolén', 50, 1),

-- Zona Sur (ID: 4) - 6 Comunas
(4, 'San Ramón', 45, 1),
(4, 'La Pintana', 55, 1),
(4, 'La Granja', 45, 1),
(4, 'La Florida', 45, 1),
(4, 'La Cisterna', 40, 1),
(4, 'El Bosque', 50, 1),

-- Zona Poniente (ID: 5) - 9 Comunas
(5, 'Quinta Normal', 40, 1),
(5, 'Pudahuel', 55, 1),
(5, 'Pedro Aguirre Cerda', 40, 1),
(5, 'Maipú', 55, 1),
(5, 'Lo Prado', 40, 1),
(5, 'Lo Espejo', 45, 1),
(5, 'Estación Central', 35, 1),
(5, 'Cerro Navia', 45, 1),
(5, 'Cerrillos', 40, 1);
-- Fase 2: soporte para Nota de Crédito Electrónica y detalle persistido de ventas.
-- Si tu base usa nombres en minúsculas, ajusta los identificadores antes de ejecutar.
USE sistemaepos;

ALTER TABLE `ventas`
    ADD COLUMN `Direccion` VARCHAR(250) NOT NULL DEFAULT '' AFTER `Giro`,
    ADD COLUMN `Comuna` VARCHAR(100) NOT NULL DEFAULT '' AFTER `Direccion`,
    ADD COLUMN `Ciudad` VARCHAR(100) NOT NULL DEFAULT '' AFTER `Comuna`,
    ADD COLUMN `EstadoDTE` VARCHAR(30) NOT NULL DEFAULT 'Aceptado_SII' AFTER `Ciudad`,
    ADD COLUMN `GlosaREF` VARCHAR(255) NULL AFTER `codigoREF`;

CREATE TABLE IF NOT EXISTS `venta_detalles` (
    `VentaDetalleID` INT NOT NULL AUTO_INCREMENT,
    `VentaID` INT NOT NULL,
    `ProductoID` INT NOT NULL,
    `CodigoBarra` VARCHAR(50) NOT NULL DEFAULT '',
    `NombreProducto` VARCHAR(150) NOT NULL DEFAULT '',
    `PrecioUnitario` DECIMAL(18,2) NOT NULL DEFAULT 0,
    `Cantidad` INT NOT NULL DEFAULT 0,
    `Subtotal` DECIMAL(18,2) NOT NULL DEFAULT 0,
    PRIMARY KEY (`VentaDetalleID`),
    KEY `IX_venta_detalles_VentaID` (`VentaID`),
    CONSTRAINT `FK_venta_detalles_ventas_VentaID`
        FOREIGN KEY (`VentaID`) REFERENCES `ventas` (`VentaID`)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

ALTER TABLE `folios`
    MODIFY COLUMN `TipoDocumento` VARCHAR(50) NOT NULL;

INSERT INTO `folios` (`TipoDocumento`, `FolioDesde`, `FolioHasta`, `FolioActual`, `Activo`)
SELECT 'Nota de Crédito Electrónica', 1, 1000, 1, 1
WHERE NOT EXISTS (
    SELECT 1 FROM `folios` WHERE `TipoDocumento` = 'Nota de Crédito Electrónica'
);
SET SQL_SAFE_UPDATES = 0;
CREATE TABLE IF NOT EXISTS `dte_referencias` (
  `IdReferencia` INT NOT NULL AUTO_INCREMENT,
  `IdTveDTEModificador` INT NOT NULL,
  `IdTveDTEOrigen` INT NOT NULL,
  `FolioOrigen` INT NOT NULL,
  `TipoDocOrigen` INT NOT NULL,
  `CodigoReferencia` INT NOT NULL DEFAULT 1,
  `RazonReferencia` VARCHAR(255) NOT NULL DEFAULT '',
  `MontoAfectado` DECIMAL(12,2) NOT NULL DEFAULT 0.00,
  `FechaRegistro` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `Usuario` VARCHAR(50) NOT NULL DEFAULT 'admin',
  PRIMARY KEY (`IdReferencia`),
  KEY `idx_ref_modificador` (`IdTveDTEModificador`),
  KEY `idx_ref_origen` (`IdTveDTEOrigen`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

INSERT INTO `folios` (`TipoDocumento`, `FolioDesde`, `FolioHasta`, `FolioActual`, `Activo`)
VALUES ('Nota de Débito Electrónica', 1, 1000, 1, 1);

-- 1. Tabla de configuración de reglas DTE (límite de boletas, políticas de emisión)
CREATE TABLE IF NOT EXISTS `configuracion_dte` (
  `IdConfig` INT NOT NULL AUTO_INCREMENT,
  `MaxBoletasPorNC` INT NOT NULL DEFAULT 10,
  `PermitirNCParcial` TINYINT(1) NOT NULL DEFAULT 1,
  `FechaModificacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `UsuarioModifica` VARCHAR(50) NOT NULL DEFAULT 'admin',
  PRIMARY KEY (`IdConfig`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- 2. Registro inicial por defecto si la tabla está vacía
INSERT INTO `configuracion_dte` (`IdConfig`, `MaxBoletasPorNC`, `PermitirNCParcial`, `UsuarioModifica`)
SELECT 1, 10, 1, 'admin'
WHERE NOT EXISTS (SELECT 1 FROM `configuracion_dte` WHERE `IdConfig` = 1);
SET SQL_SAFE_UPDATES = 1;
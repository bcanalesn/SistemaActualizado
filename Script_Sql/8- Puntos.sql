CREATE TABLE IF NOT EXISTS `puntos_cuentas` (
  `IdCliente` INT NOT NULL,
  `PuntosDisponibles` INT NOT NULL DEFAULT 0,
  `PuntosAcumuladosHistorico` INT NOT NULL DEFAULT 0,
  `PuntosCanjeadosHistorico` INT NOT NULL DEFAULT 0,
  `FechaUltimaActualizacion` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `VersionFila` INT NOT NULL DEFAULT 1,
  PRIMARY KEY (`IdCliente`),
  CONSTRAINT `FK_PuntosCuentas_Clientes` FOREIGN KEY (`IdCliente`) REFERENCES `clientes` (`IdCliente`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `puntos_movimientos` (
  `IdMovimiento` INT NOT NULL AUTO_INCREMENT,
  `IdCliente` INT NOT NULL,
  `IdTve` INT NULL,
  `Fecha` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `Tipo` VARCHAR(20) NOT NULL,
  `Puntos` INT NOT NULL,
  `MontoBase` DECIMAL(12,2) NOT NULL DEFAULT 0.00,
  `Descripcion` VARCHAR(255) NOT NULL DEFAULT '',
  `Usuario` VARCHAR(50) NOT NULL DEFAULT 'admin',
  PRIMARY KEY (`IdMovimiento`),
  KEY `IX_PuntosMovimientos_IdCliente_Fecha` (`IdCliente`, `Fecha`),
  KEY `IX_PuntosMovimientos_IdTve` (`IdTve`),
  CONSTRAINT `FK_PuntosMovimientos_Clientes` FOREIGN KEY (`IdCliente`) REFERENCES `clientes` (`IdCliente`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
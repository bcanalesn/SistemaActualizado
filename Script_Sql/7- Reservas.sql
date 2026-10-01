-- 1. Tabla principal de Reservas
CREATE TABLE IF NOT EXISTS `reserva` (
  `IdReserva` INT NOT NULL AUTO_INCREMENT,
  `CodigoReserva` VARCHAR(25) NOT NULL,
  `IdCliente` INT NOT NULL,
  `FechaRegistro` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `FechaEntregaPactada` DATETIME NOT NULL,
  `TotalPedido` DECIMAL(12,2) NOT NULL DEFAULT 0.00,
  `TotalAbonado` DECIMAL(12,2) NOT NULL DEFAULT 0.00,
  `SaldoPendiente` DECIMAL(12,2) NOT NULL DEFAULT 0.00,
  `Estado` VARCHAR(25) NOT NULL DEFAULT 'Reservado', -- 'Reservado', 'Listo para Retiro', 'Entregado', 'Anulado'
  `Observaciones` TEXT NULL,
  `UsuarioRegistro` VARCHAR(50) NOT NULL,
  `UsuarioEntrega` VARCHAR(50) NULL,
  `FechaEntregaReal` DATETIME NULL,
  `IdDTEGenerado` INT NULL,
  `StockDescontado` TINYINT(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`IdReserva`),
  UNIQUE KEY `UK_CodigoReserva` (`CodigoReserva`),
  KEY `IX_Reserva_Cliente` (`IdCliente`),
  KEY `IX_Reserva_Estado` (`Estado`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 2. Detalle de productos solicitados en el encargo
CREATE TABLE IF NOT EXISTS `reserva_detalle` (
  `IdReservaDetalle` INT NOT NULL AUTO_INCREMENT,
  `IdReserva` INT NOT NULL,
  `ProductoID` INT NOT NULL,
  `CodigoBarra` VARCHAR(50) NOT NULL,
  `NombreProducto` VARCHAR(150) NOT NULL,
  `EsPesable` TINYINT(1) NOT NULL DEFAULT 0,
  `Cantidad` INT NOT NULL,
  `PrecioUnitario` DECIMAL(12,2) NOT NULL,
  `Subtotal` DECIMAL(12,2) NOT NULL,
  `NotasItem` VARCHAR(255) NULL,
  PRIMARY KEY (`IdReservaDetalle`),
  KEY `IX_Detalle_Reserva` (`IdReserva`),
  CONSTRAINT `FK_Detalle_Reserva` FOREIGN KEY (`IdReserva`) REFERENCES `reserva` (`IdReserva`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 3. Historial de abonos e ingresos a caja
CREATE TABLE IF NOT EXISTS `reserva_pago` (
  `IdReservaPago` INT NOT NULL AUTO_INCREMENT,
  `IdReserva` INT NOT NULL,
  `FechaPago` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `Monto` DECIMAL(12,2) NOT NULL,
  `MedioPago` VARCHAR(40) NOT NULL,
  `CajaTurnoID` INT NOT NULL,
  `Usuario` VARCHAR(50) NOT NULL,
  `EsAbonoInicial` TINYINT(1) NOT NULL DEFAULT 0,
  `EsLiquidacionFinal` TINYINT(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`IdReservaPago`),
  KEY `IX_Pago_Reserva` (`IdReserva`),
  KEY `IX_Pago_CajaTurno` (`CajaTurnoID`),
  CONSTRAINT `FK_Pago_Reserva` FOREIGN KEY (`IdReserva`) REFERENCES `reserva` (`IdReserva`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
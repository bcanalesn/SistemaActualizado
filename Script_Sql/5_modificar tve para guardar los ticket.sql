SET SQL_SAFE_UPDATES = 0;
ALTER TABLE `tve2607`
  DROP COLUMN `HoraDoc`,
  ADD COLUMN `NroTicket` INT NOT NULL DEFAULT 0 AFTER `nroInT`,
  ADD COLUMN `FechaTicket` DATETIME NULL AFTER `NroTicket`;

-- Opcional: inicializar registros históricos con sus valores actuales
UPDATE `tve2607` SET `NroTicket` = `nroDTE`, `FechaTicket` = `FecDoc` WHERE `NroTicket` = 0;
SET SQL_SAFE_UPDATES = 1;
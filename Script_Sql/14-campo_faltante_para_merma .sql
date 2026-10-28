SET SQL_SAFE_UPDATES = 0;

ALTER TABLE productos ADD COLUMN StockDisponible INT NOT NULL DEFAULT 0;

-- Sincronizar los registros existentes aplicando la merma actual
UPDATE productos 
SET StockDisponible = IF(PorcentajeMerma > 0, FLOOR(Stock * (1.0 - (PorcentajeMerma / 100.0))), Stock);

SET SQL_SAFE_UPDATES = 1;
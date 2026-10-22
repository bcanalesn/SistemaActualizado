-- 1. Agregar columna OrdenPOS a productos (si no existe)
ALTER TABLE productos 
ADD COLUMN OrdenPOS INT NOT NULL DEFAULT 0;

-- 2. Inicializar el OrdenPOS con una secuencia correlativa inicial (1, 2, 3...)
SET @pos := 0;
UPDATE productos 
SET OrdenPOS = (@pos := @pos + 1)
ORDER BY Categoria ASC, Nombre ASC;

-- 3. Crear tabla independiente para preferencias del POS (evita mezclar con datos tributarios)
CREATE TABLE IF NOT EXISTS pos_preferencias (
    Id INT PRIMARY KEY DEFAULT 1,
    ModoOrdenCatalogo VARCHAR(30) NOT NULL DEFAULT 'CATEGORIA', -- 'CATEGORIA', 'PERSONALIZADO', 'MAS_VENDIDOS'
    FechaActualizacion DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

INSERT INTO pos_preferencias (Id, ModoOrdenCatalogo)
VALUES (1, 'CATEGORIA')
ON DUPLICATE KEY UPDATE Id = 1;
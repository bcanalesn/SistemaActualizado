ALTER TABLE Productos ADD COLUMN EsPesable TINYINT(1) NOT NULL DEFAULT 0;

UPDATE Productos 
SET EsPesable = 1 
WHERE LOWER(Nombre) LIKE '%(gr)%' 
   OR LOWER(Nombre) LIKE '%(g)%' 
   OR LOWER(Nombre) LIKE '%granel%';

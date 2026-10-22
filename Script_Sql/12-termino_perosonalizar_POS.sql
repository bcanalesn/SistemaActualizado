SET SQL_SAFE_UPDATES = 0;
-- 1. Recrear pos_categorias con el mismo collation de productos (utf8mb4_0900_ai_ci)

CREATE TABLE `pos_categorias` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `Nombre` VARCHAR(100) NOT NULL,
  `Orden` INT NOT NULL DEFAULT 1,
  `FondoHex` VARCHAR(10) NOT NULL DEFAULT '#F0FDF4',
  `TextoHex` VARCHAR(10) NOT NULL DEFAULT '#16A34A',
  `BordeHex` VARCHAR(10) NOT NULL DEFAULT '#15803D',
  `Activo` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UK_pos_categorias_nombre` (`Nombre`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 2. Recrear pos_familias con utf8mb4_0900_ai_ci
CREATE TABLE `pos_familias` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `CategoriaId` INT NOT NULL,
  `Nombre` VARCHAR(100) NOT NULL,
  `Orden` INT NOT NULL DEFAULT 1,
  `Activo` TINYINT(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UK_categoria_familia` (`CategoriaId`, `Nombre`),
  CONSTRAINT `FK_pos_familias_cat` FOREIGN KEY (`CategoriaId`) 
    REFERENCES `pos_categorias` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 3. Insertar las Categorías iniciales
INSERT INTO `pos_categorias` (`Nombre`, `Orden`, `FondoHex`, `TextoHex`, `BordeHex`) VALUES
('Bebidas',    1, '#FFF7ED', '#EA580C', '#C2410C'),
('Lácteos',    2, '#EFF6FF', '#2563EB', '#1D4ED8'),
('Abarrotes',  3, '#FFF7ED', '#EA580C', '#C2410C'),
('Snack',      4, '#ECFEFF', '#0891B2', '#0E7490'),
('General5',   5, '#FAF5FF', '#9333EA', '#7E22CE'),
('General6',   6, '#FDF2F8', '#DB2777', '#BE185D'),
('YGeneral4',  7, '#FEF2F2', '#DC2626', '#B91C1C'),
('ZGeneral2',  8, '#FEF2F2', '#DC2626', '#B91C1C')
ON DUPLICATE KEY UPDATE `Activo` = 1;

-- 4. Insertar las Subfamilias (ahora ambos lados usan exactamente utf8mb4_0900_ai_ci)
INSERT INTO `pos_familias` (`CategoriaId`, `Nombre`, `Orden`)
SELECT 
    c.Id AS CategoriaId,
    p.NFamilia AS Nombre,
    ROW_NUMBER() OVER (PARTITION BY c.Id ORDER BY p.NFamilia ASC) AS Orden
FROM (
    SELECT DISTINCT Categoria, NFamilia 
    FROM productos 
    WHERE Estado = 1 
      AND NFamilia IS NOT NULL 
      AND NFamilia <> '' 
      AND NFamilia <> Categoria
) p
JOIN pos_categorias c ON p.Categoria = c.Nombre
ON DUPLICATE KEY UPDATE `Activo` = 1;

SET SQL_SAFE_UPDATES = 1;
-- returning column names must be the same as the column names in the table if you want to use the result table in the input binding
CREATE OR REPLACE FUNCTION SelectProductsCost(p_cost INT) RETURNS TABLE(ProductId INT, Name VARCHAR(100), Cost INT) AS $$ BEGIN RETURN QUERY
SELECT p.ProductId,
	p.Name,
	p.Cost
FROM Products p
WHERE p.Cost = p_cost;
END;
$$ LANGUAGE 'plpgsql';
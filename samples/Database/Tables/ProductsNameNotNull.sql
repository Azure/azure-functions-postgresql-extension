CREATE TABLE ProductsNameNotNull (
	"ProductId" serial PRIMARY KEY,
	"Name" varchar(100) NOT NULL,
	"Cost" integer NULL
);
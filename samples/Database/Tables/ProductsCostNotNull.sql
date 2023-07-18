CREATE TABLE ProductsCostNotNull (
	"ProductId" serial PRIMARY KEY,
	"Name" varchar(100) NULL,
	"Cost" integer NOT NULL
);
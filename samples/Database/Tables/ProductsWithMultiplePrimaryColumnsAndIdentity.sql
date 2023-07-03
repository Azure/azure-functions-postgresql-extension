CREATE TABLE ProductsWithMultiplePrimaryColumnsAndIdentity (
	"ProductId" INT GENERATED ALWAYS AS IDENTITY,
	"ExternalId" INT NOT NULL,
	"Name" VARCHAR(100),
	"Cost" INT,
	PRIMARY KEY ("ProductId", "ExternalId")
);
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

/**
 * This function is used to test compatability with converting various data types to their respective
 * PostgreSql server types.
 */
module.exports = async function (context, req) {
  const now = new Date();
  const timeString = now.toISOString().split("T")[1].split(".")[0]; // extracts the 'HH:mm:ss' part
  const dateString = now.toISOString().split("T")[0]; // extracts the 'YYYY-MM-DD' part

  const product = {
    ProductId: parseInt(req.query.productId),
    Bigint: "9223372036854775807",
    Bigserial: "9223372036854775807",
    Bit: "1",
    BitVarying: "101",
    Boolean: true,
    Bytea: "0101",
    Character: "testCharacter",
    CharacterVarying: "testCharacterVarying",
    Date: dateString,
    DoublePrecision: 1.234567891,
    Integer: 2147483647,
    Interval: "PT1H2M3S",
    Json: { name: "John", age: 30, city: "New York" },
    Jsonb: { name: "Jane", age: 28, city: "San Francisco" },
    Numeric: 1234.56,
    Real: 1.23,
    Smallint: 32767,
    Smallserial: 32767,
    Serial: 2147483647,
    Text: "testText",
    Time: timeString,
    Timestamp: now.toISOString(),
    Uuid: "c2d29867-3d0b-d497-9191-18a9d8ee7830",
  };

  context.bindings.product = JSON.stringify(product);

  return {
    status: 201,
    body: product,
  };
};

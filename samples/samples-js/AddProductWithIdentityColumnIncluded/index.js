// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

module.exports = async function (context, req) {
  context.log("JavaScript HTTP trigger function processed a request.");

  const itemToInsert = {
    ProductId: req.query?.productId || null,
    Name: req.query?.name || null,
    Cost: req.query?.cost ? Number(req.query?.cost) : null,
  };

  context.bindings.product = JSON.stringify(itemToInsert);

  return {
    status: 201,
    body: context.bindings.product,
  };
};

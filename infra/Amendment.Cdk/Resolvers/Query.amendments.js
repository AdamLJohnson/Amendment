// Query.amendments: active or archived amendments in creation order from GSI1 (no table scan).
export function request(ctx) {
  const limit = ctx.args.limit;
  if (limit < 1 || limit > 100) {
    util.error('limit must be between 1 and 100.', 'VALIDATION_ERROR', null, { field: 'limit' });
  }
  return {
    operation: 'Query',
    index: 'GSI1',
    query: {
      expression: '#pk = :pk',
      expressionNames: { '#pk': 'GSI1PK' },
      expressionValues: util.dynamodb.toMapValues({ ':pk': ctx.args.archived ? 'AMENDMENTS#ARCHIVED' : 'AMENDMENTS#ACTIVE' })
    },
    limit: limit,
    nextToken: ctx.args.nextToken
  };
}

export function response(ctx) {
  failOnDataSourceError(ctx);
  return {
    items: ctx.result.items.map((item) => toAmendment(item, null)),
    nextToken: ctx.result.nextToken
  };
}

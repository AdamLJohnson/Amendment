// Query.amendment: metadata and every body in one strongly consistent partition query; null when missing.
export function request(ctx) {
  return {
    operation: 'Query',
    consistentRead: true,
    query: {
      expression: '#pk = :pk',
      expressionNames: { '#pk': 'PK' },
      expressionValues: util.dynamodb.toMapValues({ ':pk': 'AMENDMENT#' + ctx.args.id })
    }
  };
}

export function response(ctx) {
  failOnDataSourceError(ctx);
  const items = ctx.result.items;
  const meta = items.find((item) => item.SK === 'META');
  if (!meta) {
    return null;
  }
  return toAmendment(meta, items.filter((item) => item.SK.startsWith('BODY#')).map(toBody));
}

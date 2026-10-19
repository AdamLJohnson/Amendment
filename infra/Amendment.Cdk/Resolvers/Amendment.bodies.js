// Amendment.bodies: returns bodies already loaded by the parent resolver; otherwise queries the partition.
export function request(ctx) {
  if (ctx.source.bodies) {
    runtime.earlyReturn(ctx.source.bodies);
  }
  return {
    operation: 'Query',
    consistentRead: true,
    query: {
      expression: '#pk = :pk AND begins_with(#sk, :body)',
      expressionNames: { '#pk': 'PK', '#sk': 'SK' },
      expressionValues: util.dynamodb.toMapValues({ ':pk': 'AMENDMENT#' + ctx.source.id, ':body': 'BODY#' })
    }
  };
}

export function response(ctx) {
  failOnDataSourceError(ctx);
  return ctx.result.items.map(toBody);
}

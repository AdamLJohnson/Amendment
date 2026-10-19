// Every Mutation field: forwards the field name, arguments and verified Cognito identity to the API Lambda
// (ApiRequest in src/Amendment.Api.Lambda) and maps its error object to a GraphQL error with errorType.
export function request(ctx) {
  const identity = ctx.identity || {};
  return {
    operation: 'Invoke',
    payload: {
      field: ctx.info.fieldName,
      arguments: ctx.args,
      identity: { sub: identity.sub || null, groups: identity.groups || [] }
    }
  };
}

export function response(ctx) {
  if (ctx.error) {
    util.error('The operation failed.', 'INTERNAL_ERROR');
  }
  const error = ctx.result.error;
  if (error) {
    util.error(error.message, error.type, null, { field: error.field, errors: error.errors });
  }
  return ctx.result.data;
}

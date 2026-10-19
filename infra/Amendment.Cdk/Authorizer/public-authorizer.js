// AppSync Lambda authorizer for anonymous projectors (decision D21). It accepts only the fixed, non-secret public
// token; the schema's @aws_lambda directives limit such callers to the public fields. AppSync rejects tokens that do
// not match the validation regex before calling this function, and caches each answer per token.
const TOKEN = process.env.PUBLIC_TOKEN;
const TTL_SECONDS = 3600;

exports.handler = async (event) => ({
  isAuthorized: typeof TOKEN === 'string' && TOKEN.length > 0 && event.authorizationToken === TOKEN,
  ttlOverride: TTL_SECONDS
});

// Query.languages: configured languages (SYSTEM / LANGUAGE#<code>) in display order.
export function request(ctx) {
  return {
    operation: 'Query',
    query: {
      expression: '#pk = :pk AND begins_with(#sk, :prefix)',
      expressionNames: { '#pk': 'PK', '#sk': 'SK' },
      expressionValues: util.dynamodb.toMapValues({ ':pk': 'SYSTEM', ':prefix': 'LANGUAGE#' })
    }
  };
}

// Insertion sort: APPSYNC_JS rejects an arrow comparator and ignores a function comparator in Array.sort.
function bySortOrder(languages) {
  const sorted = [];
  for (const language of languages) {
    const index = sorted.findIndex((l) => l.sortOrder > language.sortOrder);
    if (index < 0) {
      sorted.push(language);
    } else {
      sorted.splice(index, 0, language);
    }
  }
  return sorted;
}

export function response(ctx) {
  failOnDataSourceError(ctx);
  return bySortOrder(ctx.result.items.map((item) => ({ code: item.code, name: item.name, sortOrder: item.sortOrder })));
}

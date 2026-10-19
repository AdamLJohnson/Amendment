// Shared prelude for the APPSYNC_JS resolvers. ApiStack prepends this file to each resolver file, because
// APPSYNC_JS cannot import local modules. Item attribute names follow ItemMapper (src/Amendment.Infrastructure).
import { util, runtime } from '@aws-appsync/utils';

const BODY_STATUS = { New: 'NEW', Draft: 'DRAFT', UnderReview: 'UNDER_REVIEW', Ready: 'READY' };
const SLIDE_DELIMITER = '**NEWSLIDE**';

// Stored timestamps have 7 fractional digits; the API returns milliseconds, like the Lambda.
function timestamp(value) {
  return value.length > 24 ? value.substring(0, 23) + 'Z' : value;
}

function audit(item, prefix) {
  return { by: item[prefix + 'By'], at: timestamp(item[prefix + 'At']) };
}

// Same rule as SlideDeck.PageCount: empty markdown has no pages.
function pageCount(markdown) {
  return markdown ? markdown.split(SLIDE_DELIMITER).length : 0;
}

function toBody(item) {
  return {
    amendmentId: item.amendmentId,
    language: item.language,
    markdown: item.markdown,
    status: BODY_STATUS[item.status],
    pageCount: pageCount(item.markdown),
    version: '' + item.version,
    created: audit(item, 'created'),
    updated: audit(item, 'updated')
  };
}

function toAmendment(item, bodies) {
  const amendment = {
    id: item.id,
    title: item.title,
    author: item.author,
    motion: item.motion,
    source: item.source,
    legisId: item.legisId,
    primaryLanguage: item.primaryLanguage,
    isArchived: item.isArchived === true,
    isApproved: item.isApproved === true,
    parentAmendmentId: item.parentAmendmentId,
    version: '' + item.version,
    created: audit(item, 'created'),
    updated: audit(item, 'updated')
  };
  if (bodies) {
    amendment.bodies = bodies;
  }
  return amendment;
}

function failOnDataSourceError(ctx) {
  if (ctx.error) {
    util.error('The data could not be read.', 'INTERNAL_ERROR');
  }
}

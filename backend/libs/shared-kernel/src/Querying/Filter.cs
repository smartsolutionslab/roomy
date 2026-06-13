using SmartSolutionsLab.Roomy.SharedKernel.Pagination;

namespace SmartSolutionsLab.Roomy.SharedKernel.Querying;

// Base query input for a paged list: carries the pagination, which every list shares. A concrete
// per-element filter derives from this and adds that element's own criteria (and free-text search),
// so the search/paging plumbing stays in one place across employees and other searchable elements.
public abstract record Filter(PageRequest Page);

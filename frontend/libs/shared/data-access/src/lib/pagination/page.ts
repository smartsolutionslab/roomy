// One slice of a keyset-paginated list as the SPA models it: items already mapped to their branded
// domain type, plus the opaque cursor that fetches the next slice — null at the end of the list. A
// data-access facade maps the wire envelope ({ items, nextCursor }) to this, so features never touch
// the generated DTOs.
export interface Page<T> {
  readonly items: T[];
  readonly nextCursor: string | null;
}

export function mapPage<TSource, TTarget>(
  source: { items: TSource[]; nextCursor?: string | null },
  project: (item: TSource) => TTarget,
): Page<TTarget> {
  return { items: source.items.map(project), nextCursor: source.nextCursor ?? null };
}

// The brand is a phantom property: it lives only in the type system and is erased at
// runtime, so a Brand<T, B> costs nothing and a plain T cannot be passed where a brand
// is expected. Mint branded values through a validating smart constructor, not a bare
// `as` cast spread across the code.
declare const brand: unique symbol;

export type Brand<T, B extends string> = T & { readonly [brand]: B };

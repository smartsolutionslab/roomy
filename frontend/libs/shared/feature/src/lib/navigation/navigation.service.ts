import { Injectable, computed, inject } from '@angular/core';
import { Route, Router } from '@angular/router';
import { SessionService } from '@roomy/shared-data-access';
import { IconName } from '@roomy/shared-ui';

import { adminGuard } from '../auth/admin.guard';

// Presentation metadata a route opts into to appear in the navigation. It carries no role — a route's
// access rule stays in its `canActivate` guard, which the builder reads instead. Attach it as
// `data: { nav: { ... } satisfies NavMeta }` on the route.
export interface NavMeta {
  readonly labelKey: string;
  readonly icon: IconName;
  readonly order: number;
  readonly descKey?: string;
}

// A resolved navigation entry: the route's full path plus its presentation metadata, with `requiresAdmin`
// inferred from whether the route is reached through `adminGuard`.
export interface NavItem {
  readonly path: string;
  readonly labelKey: string;
  readonly icon: IconName;
  readonly descKey?: string;
  readonly order: number;
  readonly requiresAdmin: boolean;
}

// Builds the signed-in navigation from the router configuration: the single source of truth is the
// routes themselves. Every route that declares `data.nav` becomes an entry; its visibility follows the
// guard it already carries (a route guarded by `adminGuard` is administrator-only), so a link can never
// diverge from who the route actually admits. The lists are role-filtered against the BFF session.
@Injectable({ providedIn: 'root' })
export class NavigationService {
  private readonly session = inject(SessionService);
  private readonly allItems = this.build(inject(Router).config).sort(
    (left, right) => left.order - right.order,
  );

  readonly items = computed<readonly NavItem[]>(() => {
    const isAdministrator = this.session.isAdministrator();
    return this.allItems.filter((item) => isAdministrator || !item.requiresAdmin);
  });

  readonly mainItems = computed(() => this.items().filter((item) => !item.requiresAdmin));
  readonly adminItems = computed(() => this.items().filter((item) => item.requiresAdmin));

  private build(routes: readonly Route[], parentPath = '', inheritedAdmin = false): NavItem[] {
    const items: NavItem[] = [];

    for (const route of routes) {
      const requiresAdmin = inheritedAdmin || (route.canActivate?.includes(adminGuard) ?? false);
      const fullPath = [parentPath, route.path].filter(Boolean).join('/');
      const meta = route.data?.['nav'] as NavMeta | undefined;

      if (meta) {
        items.push({
          path: `/${fullPath}`,
          labelKey: meta.labelKey,
          icon: meta.icon,
          descKey: meta.descKey,
          order: meta.order,
          requiresAdmin,
        });
      }

      if (route.children) {
        items.push(...this.build(route.children, fullPath, requiresAdmin));
      }
    }

    return items;
  }
}

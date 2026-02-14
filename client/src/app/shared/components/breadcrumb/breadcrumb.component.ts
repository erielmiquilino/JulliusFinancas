import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { Subject } from 'rxjs';
import { filter, map, takeUntil } from 'rxjs/operators';

export interface BreadcrumbItem {
  label: string;
  url?: string;
  icon?: string;
  enabled?: boolean;
  active?: boolean;
}

@Component({
  selector: 'app-breadcrumb',
  template: `
    <nav class="breadcrumb-nav" *ngIf="breadcrumbs.length > 1">
      <span class="breadcrumb" *ngFor="let item of breadcrumbs; let last = last">
        <a *ngIf="!last && item.url && item.enabled !== false"
           (click)="navigateTo(item.url)"
           class="breadcrumb-link"
           [class.disabled]="item.enabled === false">
          {{item.label}}
        </a>
        <span *ngIf="last || !item.url || item.enabled === false"
              class="breadcrumb-current"
              [class.disabled]="item.enabled === false">
          {{item.label}}
        </span>
        <span *ngIf="!last" class="breadcrumb-separator"> > </span>
      </span>
    </nav>
  `,
  styles: [`
    .breadcrumb-nav {
      padding: 8px 0;
      margin-bottom: 16px;
      font-size: 14px;
    }

    .breadcrumb {
      display: inline;
    }

    .breadcrumb-link {
      color: #1976d2;
      text-decoration: none;
      padding: 4px;
      border-radius: 4px;
      transition: background-color 0.2s;
      cursor: pointer;
    }

    .breadcrumb-link:hover:not(.disabled) {
      background-color: rgba(25, 118, 210, 0.1);
    }

    .breadcrumb-link.disabled {
      color: #ccc;
      cursor: not-allowed;
    }

    .breadcrumb-current {
      color: #666;
      font-weight: 500;
    }

    .breadcrumb-current.disabled {
      color: #ccc;
    }

    .breadcrumb-separator {
      color: #999;
      margin: 0 8px;
    }
  `]
})
export class BreadcrumbComponent implements OnInit, OnDestroy {
  breadcrumbs: BreadcrumbItem[] = [];
  private destroy$ = new Subject<void>();

  private routeLabels: { [key: string]: string } = {
    'cards': 'Cartões',
    'transactions': 'Transações'
  };

  constructor(private router: Router) {}

  ngOnInit(): void {
    this.router.events
      .pipe(
        filter(event => event instanceof NavigationEnd),
        map(() => this.buildBreadcrumbs()),
        takeUntil(this.destroy$)
      )
      .subscribe(breadcrumbs => {
        this.breadcrumbs = breadcrumbs;
      });

    this.breadcrumbs = this.buildBreadcrumbs();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private buildBreadcrumbs(): BreadcrumbItem[] {
    const url = this.router.url;
    const segments = url.split('/').filter(segment => segment);
    const breadcrumbs: BreadcrumbItem[] = [];

    breadcrumbs.push({
      label: 'Dashboard',
      url: '/dashboard',
      enabled: true,
      active: url === '/dashboard'
    });

    let currentUrl = '';
    segments.forEach((segment, index) => {
      currentUrl += `/${segment}`;

      if (this.isIdSegment(segment)) return;

      const label = this.routeLabels[segment];
      if (label) {
        const isLast = index === segments.length - 1;
        const item: BreadcrumbItem = {
          label,
          url: isLast ? undefined : currentUrl,
          enabled: this.isRouteEnabled(segment, currentUrl),
          active: isLast
        };

        breadcrumbs.push(item);
      }
    });

    return breadcrumbs;
  }

  private isIdSegment(segment: string): boolean {
    const guidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
    return guidRegex.test(segment);
  }

  private isRouteEnabled(segment: string, url: string): boolean {
    return true;
  }

  navigateTo(url: string): void {
    this.router.navigate([url]);
  }
}

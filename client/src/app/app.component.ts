import { Component, inject, ViewChild, OnInit, OnDestroy } from '@angular/core';
import { Router, NavigationEnd, RouterModule } from '@angular/router';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { MatSidenav, MatSidenavModule } from '@angular/material/sidenav';
import { CommonModule } from '@angular/common';
import { filter, takeUntil } from 'rxjs/operators';
import { Subject } from 'rxjs';
import { HeaderComponent } from './layout/header/header.component';
import { SideMenuComponent } from './layout/side-menu/side-menu.component';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatSidenavModule,
    HeaderComponent,
    SideMenuComponent
  ]
})
export class AppComponent implements OnInit, OnDestroy {
  private router = inject(Router);
  private breakpointObserver = inject(BreakpointObserver);
  private destroy$ = new Subject<void>();

  @ViewChild('sidenav') sidenav!: MatSidenav;

  showLayout = true;
  isMobile = false;

  constructor() {
    // Inicializa showLayout com base na URL atual para evitar NG0100
    this.showLayout = !this.router.url.includes('/auth');

    // Monitora mudanças de rota para decidir quando mostrar o layout
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe((event: NavigationEnd) => {
      // Esconde o layout nas rotas de autenticação
      this.showLayout = !event.url.includes('/auth');

      // Fecha o sidenav no mobile após navegação
      if (this.isMobile && this.sidenav) {
        this.sidenav.close();
      }
    });
  }

  ngOnInit(): void {
    // Observa breakpoints do Material Design (Handset = Compact window class)
    this.breakpointObserver
      .observe([Breakpoints.Handset])
      .pipe(takeUntil(this.destroy$))
      .subscribe(result => {
        this.isMobile = result.matches;
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  toggleSidenav(): void {
    this.sidenav.toggle();
  }

  onSidenavItemClick(): void {
    if (this.isMobile && this.sidenav) {
      this.sidenav.close();
    }
  }
}

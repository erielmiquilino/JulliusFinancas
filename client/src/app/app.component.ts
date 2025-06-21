import { Component, inject } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {
  private router = inject(Router);
  showLayout = true;

  constructor() {
    // Monitora mudanças de rota para decidir quando mostrar o layout
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe((event: NavigationEnd) => {
      // Esconde o layout nas rotas de autenticação
      this.showLayout = !event.url.includes('/auth');
    });
  }
}

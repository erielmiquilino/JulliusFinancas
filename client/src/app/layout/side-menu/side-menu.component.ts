import { Component } from '@angular/core';

interface MenuItem {
  name: string;
  route: string;
}

@Component({
  selector: 'app-side-menu',
  templateUrl: './side-menu.component.html',
  styleUrls: ['./side-menu.component.scss']
})
export class SideMenuComponent {
  menuItems: MenuItem[] = [
    { name: 'Dashboard', route: '/dashboard' },
    { name: 'Transactions', route: '/transactions' },
    { name: 'Grupos', route: '/groups' },
    { name: 'Contas', route: '/accounts' }
  ];
}

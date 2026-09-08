import { Routes } from '@angular/router';

import { LoginComponent } from './pages/login/login.component';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { ProductosComponent } from './pages/productos/productos.component';
import { MenuDiarioComponent } from './pages/menu-diario/menu-diario.component';
import { VentasComponent } from './pages/ventas/ventas.component';
import { UsuariosComponent } from './pages/usuarios/usuarios.component';
import { ReportesComponent } from './pages/reportes/reportes.component';
import { HistorialVentasComponent } from './pages/historial-ventas/historial-ventas.component';

import { MainLayoutComponent } from './layout/main-layout/main-layout.component';

import { authGuard } from './guards/auth.guard';
import { adminGuard } from './guards/admin.guard';


export const routes: Routes = [

  {
    path: 'login',
    component: LoginComponent
  },

  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [authGuard],

    children: [

      {
        path: 'dashboard',
        component: DashboardComponent
      },

      {
        path: 'ventas',
        component: VentasComponent
      },

      {
        path: 'productos',
        component: ProductosComponent
      },

      {
        path: 'menu-diario',
        component: MenuDiarioComponent,
        canActivate: [adminGuard]
      },

      {
        path: 'reportes',
        component: ReportesComponent
      },

      {
        path: 'usuarios',
        component: UsuariosComponent,
        canActivate: [adminGuard]
      },

      {
        path: 'historial-ventas',
        component: HistorialVentasComponent,
        canActivate: [adminGuard]
      },

      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full'
      }

    ]
  },

  {
    path: '**',
    redirectTo: 'login'
  }

];
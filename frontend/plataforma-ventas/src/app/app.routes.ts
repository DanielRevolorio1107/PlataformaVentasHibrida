import { Routes } from '@angular/router';
import { LoginComponent } from './pages/login/login.component';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { ProductosComponent } from './pages/productos/productos.component';
import { MenuDiarioComponent } from './pages/menu-diario/menu-diario.component';
import { VentasComponent } from './pages/ventas/ventas.component';
import { UsuariosComponent } from './pages/usuarios/usuarios.component';
import { ReportesComponent } from './pages/reportes/reportes.component';
import { HistorialVentasComponent } from './pages/historial-ventas/historial-ventas.component';
import { authGuard } from './guards/auth.guard';
import { adminGuard } from './guards/admin.guard';


export const routes: Routes = [
  {
    path: 'login',
    component: LoginComponent
  },
  {
    path: 'dashboard',
    component: DashboardComponent,
    canActivate: [authGuard]
  },
  {
    path: 'productos',
    component: ProductosComponent,
    canActivate: [authGuard]
  },
  {
    path: 'menu-diario',
    component: MenuDiarioComponent,
    canActivate: [authGuard, adminGuard]
  },
 {
  path: 'ventas',
  component: VentasComponent,
  canActivate: [authGuard]
},
{
  path: 'usuarios',
  component: UsuariosComponent,
  canActivate: [authGuard, adminGuard]
},
{
  path: 'reportes',
  component: ReportesComponent,
  canActivate: [authGuard]
},
{
  path: 'historial-ventas',
  component: HistorialVentasComponent,
  canActivate: [authGuard, adminGuard]
},
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full'
  },
  {
    path: '**',
    redirectTo: 'login'
  }
];
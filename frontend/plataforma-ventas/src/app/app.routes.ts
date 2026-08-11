import { Routes } from '@angular/router';
import { LoginComponent } from './pages/login/login.component';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { ProductosComponent } from './pages/productos/productos.component';
import { MenuDiarioComponent } from './pages/menu-diario/menu-diario.component';
import { VentasComponent } from './pages/ventas/ventas.component';
import { UsuariosComponent } from './pages/usuarios/usuarios.component';
import { ReportesComponent } from './pages/reportes/reportes.component';
import { HistorialVentasComponent } from './pages/historial-ventas/historial-ventas.component';

export const routes: Routes = [
  {
    path: 'login',
    component: LoginComponent
  },
  {
    path: 'dashboard',
    component: DashboardComponent
  },
  {
    path: 'productos',
    component: ProductosComponent
  },
  {
    path: 'menu-diario',
    component: MenuDiarioComponent
 },
 {
  path: 'ventas',
  component: VentasComponent
},
{
  path: 'usuarios',
  component: UsuariosComponent
},
{
  path: 'reportes',
  component: ReportesComponent
},
{
  path: 'historial-ventas',
  component: HistorialVentasComponent
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
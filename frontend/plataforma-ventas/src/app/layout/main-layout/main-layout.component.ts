import {
  Component,
  OnDestroy,
  OnInit
} from '@angular/core';

import { CommonModule } from '@angular/common';

import {
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet
} from '@angular/router';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    RouterLinkActive,
    RouterOutlet
  ],
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.scss'
})
export class MainLayoutComponent
  implements OnInit, OnDestroy {

  usuario: any;

  estaEnLinea = navigator.onLine;

  fechaActual = new Date();

  private actualizarEstadoConexion = () => {
    this.estaEnLinea = navigator.onLine;
  };

  constructor(
    private router: Router
  ) {

    const usuarioGuardado =
      localStorage.getItem('usuario');

    if (usuarioGuardado) {
      this.usuario =
        JSON.parse(usuarioGuardado);
    }
  }

  ngOnInit(): void {

    window.addEventListener(
      'online',
      this.actualizarEstadoConexion
    );

    window.addEventListener(
      'offline',
      this.actualizarEstadoConexion
    );
  }

  ngOnDestroy(): void {

    window.removeEventListener(
      'online',
      this.actualizarEstadoConexion
    );

    window.removeEventListener(
      'offline',
      this.actualizarEstadoConexion
    );
  }

  cerrarSesion(): void {

    localStorage.removeItem('token');

    localStorage.removeItem('usuario');

    this.router.navigate(['/login']);
  }
}
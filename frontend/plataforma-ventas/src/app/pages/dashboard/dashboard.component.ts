import {
  Component,
  OnDestroy,
  OnInit
} from '@angular/core';

import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';

import { environment } from '../../../enviroments/enviromet';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent
  implements OnInit, OnDestroy {

  usuario: any;

  totalProductos = 0;
  totalIngresos = 0;
  totalVentas = 0;

  estaEnLinea = navigator.onLine;

  private apiUrl =
    `${environment.apiUrl}/reportes/ventas-hoy`;

  private productosUrl =
    `${environment.apiUrl}/productos`;

  private actualizarEstadoConexion = () => {
    this.estaEnLinea = navigator.onLine;
  };

  constructor(
    private http: HttpClient
  ) {
    const usuarioGuardado =
      localStorage.getItem('usuario');

    if (usuarioGuardado) {
      this.usuario =
        JSON.parse(usuarioGuardado);
    }
  }

  ngOnInit(): void {
    this.cargarVentasHoy();
    this.cargarProductos();

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

  get ticketPromedio(): number {
    if (this.totalVentas === 0) {
      return 0;
    }

    return this.totalIngresos /
      this.totalVentas;
  }

  cargarProductos(): void {
    this.http.get<any[]>(
      this.productosUrl
    ).subscribe({
      next: (productos) => {
        this.totalProductos =
          productos.filter(
            producto => producto.activo
          ).length;
      },

      error: (error) => {
        console.error(
          'Error al cargar productos',
          error
        );
      }
    });
  }

  cargarVentasHoy(): void {
    this.http.get<any>(
      this.apiUrl
    ).subscribe({
      next: (respuesta) => {
        this.totalIngresos =
          respuesta.totalIngresos;

        this.totalVentas =
          respuesta.totalVentas;
      },

      error: (error) => {
        console.error(
          'Error al cargar ventas del día',
          error
        );
      }
    });
  }
}
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
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
export class DashboardComponent implements OnInit {

  usuario: any;

  totalProductos = 0;
  totalIngresos = 0;
  totalVentas = 0;

  private apiUrl =
  `${environment.apiUrl}/reportes/ventas-hoy`;

  private productosUrl =
  `${environment.apiUrl}/productos`;

  constructor(
    private router: Router,
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


  cerrarSesion(): void {

    localStorage.removeItem('token');
    localStorage.removeItem('usuario');

    this.router.navigate(['/login']);
  }
}
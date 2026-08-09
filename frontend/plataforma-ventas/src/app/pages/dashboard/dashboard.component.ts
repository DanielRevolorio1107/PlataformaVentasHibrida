import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {

  usuario: any;
  totalProductos = 0;
  totalIngresos = 0;
  totalVentas = 0;

  private apiUrl = 'http://localhost:5080/api/reportes/ventas-hoy';
  private productosUrl = 'http://localhost:5080/api/productos';
  
  constructor(
    private router: Router,
    private http: HttpClient
  ) {
    const usuarioGuardado = localStorage.getItem('usuario');

    if (usuarioGuardado) {
      this.usuario = JSON.parse(usuarioGuardado);
    }
  }

  ngOnInit(): void {
    this.cargarVentasHoy();
    this.cargarProductos();  
  }

  cargarProductos() {
  const token = localStorage.getItem('token');

  const headers = new HttpHeaders({
    Authorization: `Bearer ${token}`
  });

  this.http.get<any[]>(this.productosUrl, { headers }).subscribe({
    next: (productos) => {
      this.totalProductos = productos.filter(p => p.activo).length;
    },
    error: (error) => {
      console.error('Error al cargar productos', error);
    }
  });
}

  cargarVentasHoy() {
    const token = localStorage.getItem('token');

    const headers = new HttpHeaders({
      Authorization: `Bearer ${token}`
    });

    this.http.get<any>(this.apiUrl, { headers }).subscribe({
      next: (respuesta) => {
        this.totalIngresos = respuesta.totalIngresos;
        this.totalVentas = respuesta.totalVentas;
      },
      error: (error) => {
        console.error('Error al cargar ventas del día', error);
      }
    });
  }

  cerrarSesion() {
    localStorage.removeItem('token');
    localStorage.removeItem('usuario');

    this.router.navigate(['/login']);
  }
}
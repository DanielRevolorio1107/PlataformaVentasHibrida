import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-ventas',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './ventas.component.html',
  styleUrl: './ventas.component.scss'
})
export class VentasComponent implements OnInit {

  menu: any = null;
  metodosPago: any[] = [];

  metodoPagoId = '';
  observaciones = '';

  mensaje = '';
  cargando = false;

  private menusUrl = 'http://localhost:5080/api/menus/hoy';
  private metodosPagoUrl = 'http://localhost:5080/api/metodospago';
  private ventasUrl = 'http://localhost:5080/api/ventas';

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.cargarMenu();
    this.cargarMetodosPago();
  }

  obtenerHeaders(): HttpHeaders {
    const token = localStorage.getItem('token');

    return new HttpHeaders({
      Authorization: `Bearer ${token}`
    });
  }

  cargarMenu(): void {
    this.http.get<any>(
      this.menusUrl,
      { headers: this.obtenerHeaders() }
    ).subscribe({
      next: (menu) => {

        menu.productos = menu.productos
          .filter((p: any) => p.disponible)
          .map((p: any) => ({
            ...p,
            cantidad: 0
          }));

        this.menu = menu;
      },

      error: (error) => {

        if (error.status === 404) {
          this.mensaje = 'No existe un menú disponible para hoy.';
          return;
        }

        this.mensaje = 'No se pudo cargar el menú.';
      }
    });
  }

  cargarMetodosPago(): void {
    this.http.get<any[]>(
      this.metodosPagoUrl,
      { headers: this.obtenerHeaders() }
    ).subscribe({
      next: (metodos) => {
        this.metodosPago = metodos;
      },

      error: () => {
        this.mensaje = 'No se pudieron cargar los métodos de pago.';
      }
    });
  }

  aumentarCantidad(producto: any): void {
    producto.cantidad++;
  }

  disminuirCantidad(producto: any): void {
    if (producto.cantidad > 0) {
      producto.cantidad--;
    }
  }

  calcularTotal(): number {

    if (!this.menu) {
      return 0;
    }

    return this.menu.productos.reduce(
      (total: number, producto: any) =>
        total + (producto.precio * producto.cantidad),
      0
    );
  }

  registrarVenta(): void {

  const detalles = this.menu.productos
    .filter((producto: any) => producto.cantidad > 0)
    .map((producto: any) => ({
      productoId: producto.productoId,
      cantidad: producto.cantidad
    }));

  if (detalles.length === 0) {
    this.mensaje = 'Selecciona al menos un producto.';
    return;
  }

  if (!this.metodoPagoId) {
    this.mensaje = 'Selecciona un método de pago.';
    return;
  }

  this.cargando = true;
  this.mensaje = '';

  const datos = {
    metodoPagoId: this.metodoPagoId,
    observaciones: this.observaciones,
    detalles: detalles
  };

  this.http.post<any>(
    this.ventasUrl,
    datos,
    { headers: this.obtenerHeaders() }
  ).subscribe({

    next: (respuesta) => {

      this.mensaje =
        `Venta registrada correctamente. Total: Q ${respuesta.total.toFixed(2)}`;

      this.cargando = false;

      // Limpiar formulario
      this.menu.productos.forEach((producto: any) => {
        producto.cantidad = 0;
      });

      this.metodoPagoId = '';
      this.observaciones = '';
    },

    error: (error) => {

      this.cargando = false;

      if (error.error?.mensaje) {
        this.mensaje = error.error.mensaje;
      } else {
        this.mensaje = 'No se pudo registrar la venta.';
      }
    }
  });
}
}
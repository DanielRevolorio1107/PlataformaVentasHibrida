import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { environment } from '../../../enviroments/enviromet';

@Component({
  selector: 'app-ventas',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink
  ],
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

  private menusUrl =
  `${environment.apiUrl}/menus/hoy`;

  private metodosPagoUrl =
  `${environment.apiUrl}/metodospago`;

  private ventasUrl =
  `${environment.apiUrl}/ventas`;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.cargarMenu();
    this.cargarMetodosPago();
  }


  cargarMenu(): void {

    this.http.get<any>(
      this.menusUrl
    ).subscribe({

      next: (menu) => {

        menu.productos = menu.productos
          .filter(
            (producto: any) =>
              producto.disponible
          )
          .map(
            (producto: any) => ({
              ...producto,
              cantidad: 0
            })
          );

        this.menu = menu;
      },

      error: (error) => {

        if (error.status === 404) {

          this.menu = null;

          this.mensaje =
            'No existe un menú disponible para hoy.';

          return;
        }

        this.mensaje =
          'No se pudo cargar el menú.';
      }

    });
  }


  cargarMetodosPago(): void {

    this.http.get<any[]>(
      this.metodosPagoUrl
    ).subscribe({

      next: (metodos) => {

        this.metodosPago = metodos;
      },

      error: () => {

        this.mensaje =
          'No se pudieron cargar los métodos de pago.';
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
      (
        total: number,
        producto: any
      ) =>
        total +
        (producto.precio * producto.cantidad),
      0
    );
  }


  registrarVenta(): void {

    if (!this.menu) {

      this.mensaje =
        'No existe un menú disponible para registrar la venta.';

      return;
    }

    const detalles = this.menu.productos
      .filter(
        (producto: any) =>
          producto.cantidad > 0
      )
      .map(
        (producto: any) => ({
          productoId: producto.productoId,
          cantidad: producto.cantidad
        })
      );

    if (detalles.length === 0) {

      this.mensaje =
        'Selecciona al menos un producto.';

      return;
    }

    if (!this.metodoPagoId) {

      this.mensaje =
        'Selecciona un método de pago.';

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
      datos
    ).subscribe({

      next: (respuesta) => {

        this.mensaje =
          `Venta registrada correctamente. Total: Q ${respuesta.total.toFixed(2)}`;

        this.cargando = false;
        this.menu.productos.forEach(
          (producto: any) => {
            producto.cantidad = 0;
          }
        );
        this.metodoPagoId = '';
        this.observaciones = '';
      },

      error: (error) => {

        this.cargando = false;

        if (error.error?.mensaje) {

          this.mensaje =
            error.error.mensaje;

        } else {

          this.mensaje =
            'No se pudo registrar la venta.';
        }
      }

    });
  }
}
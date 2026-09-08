import {
  Component,
  OnInit
} from '@angular/core';

import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

import { environment } from '../../../enviroments/enviromet';

@Component({
  selector: 'app-reportes',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './reportes.component.html',
  styleUrl: './reportes.component.scss'
})
export class ReportesComponent implements OnInit {

  ventasHoy: any = null;

  ingresosMetodoPago: any[] = [];

  productosMasVendidos: any[] = [];

  ventasPorFecha: any[] = [];

  fechaConsulta =
    this.obtenerFechaLocal();

  mensaje = '';

  cargando = false;

  private apiUrl =
    `${environment.apiUrl}/reportes`;

  constructor(
    private http: HttpClient
  ) {}

  ngOnInit(): void {

    this.cargarVentasHoy();

    this.cargarIngresosMetodoPago();

    this.cargarProductosMasVendidos();

    this.consultarVentasPorFecha();
  }

  get ticketPromedio(): number {

    const totalVentas =
      this.ventasHoy?.totalVentas || 0;

    const totalIngresos =
      this.ventasHoy?.totalIngresos || 0;

    if (totalVentas === 0) {
      return 0;
    }

    return totalIngresos / totalVentas;
  }

  get totalIngresosConsulta(): number {

    return this.ventasPorFecha.reduce(
      (total, venta) =>
        total + (venta.total || 0),
      0
    );
  }

  get ventasRegistradasConsulta(): number {

    return this.ventasPorFecha.filter(
      venta =>
        venta.estado === 'REGISTRADA'
    ).length;
  }

  get ventasAnuladasConsulta(): number {

    return this.ventasPorFecha.filter(
      venta =>
        venta.estado === 'ANULADA'
    ).length;
  }

  get mayorCantidadVendida(): number {

    if (
      this.productosMasVendidos.length === 0
    ) {
      return 0;
    }

    return Math.max(
      ...this.productosMasVendidos.map(
        producto =>
          producto.cantidadVendida || 0
      )
    );
  }

  obtenerFechaLocal(): string {

    const hoy = new Date();

    const anio =
      hoy.getFullYear();

    const mes =
      String(
        hoy.getMonth() + 1
      ).padStart(2, '0');

    const dia =
      String(
        hoy.getDate()
      ).padStart(2, '0');

    return `${anio}-${mes}-${dia}`;
  }

  porcentajeMetodo(
    metodo: any
  ): number {

    const total =
      this.ingresosMetodoPago.reduce(
        (suma, item) =>
          suma + (item.totalIngresos || 0),
        0
      );

    if (total === 0) {
      return 0;
    }

    return (
      metodo.totalIngresos /
      total
    ) * 100;
  }

  porcentajeProducto(
    producto: any
  ): number {

    if (
      this.mayorCantidadVendida === 0
    ) {
      return 0;
    }

    return (
      producto.cantidadVendida /
      this.mayorCantidadVendida
    ) * 100;
  }

  cargarVentasHoy(): void {

    this.http.get<any>(
      `${this.apiUrl}/ventas-hoy`
    ).subscribe({

      next: (respuesta) => {

        this.ventasHoy =
          respuesta;
      },

      error: () => {

        this.mensaje =
          'No se pudo cargar el resumen de ventas.';
      }

    });
  }

  cargarIngresosMetodoPago(): void {

    this.http.get<any>(
      `${this.apiUrl}/ingresos-metodo-pago`
    ).subscribe({

      next: (respuesta) => {

        this.ingresosMetodoPago =
          respuesta.resumen;
      },

      error: () => {

        this.mensaje =
          'No se pudieron cargar los ingresos por método de pago.';
      }

    });
  }

  cargarProductosMasVendidos(): void {

    this.http.get<any>(
      `${this.apiUrl}/productos-mas-vendidos`
    ).subscribe({

      next: (respuesta) => {

        this.productosMasVendidos =
          respuesta.productos;
      },

      error: () => {

        this.mensaje =
          'No se pudieron cargar los productos más vendidos.';
      }

    });
  }

  consultarVentasPorFecha(): void {

    if (!this.fechaConsulta) {

      this.mensaje =
        'Selecciona una fecha.';

      return;
    }

    this.cargando = true;

    this.mensaje = '';

    this.http.get<any>(
      `${this.apiUrl}/ventas-por-fecha?fecha=${this.fechaConsulta}`
    ).subscribe({

      next: (respuesta) => {

        this.ventasPorFecha =
          respuesta.ventas;

        this.cargando = false;
      },

      error: () => {

        this.mensaje =
          'No se pudieron consultar las ventas de la fecha.';

        this.cargando = false;
      }

    });
  }
}
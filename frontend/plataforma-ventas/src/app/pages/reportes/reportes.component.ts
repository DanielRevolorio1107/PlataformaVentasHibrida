import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { environment } from '../../../enviroments/enviromet';

@Component({
  selector: 'app-reportes',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink
  ],
  templateUrl: './reportes.component.html',
  styleUrl: './reportes.component.scss'
})
export class ReportesComponent implements OnInit {

  ventasHoy: any = null;
  ingresosMetodoPago: any[] = [];
  productosMasVendidos: any[] = [];
  ventasPorFecha: any[] = [];

  fechaConsulta = this.obtenerFechaLocal();

  mensaje = '';
  cargando = false;

  private apiUrl = `${environment.apiUrl}/reportes`;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.cargarVentasHoy();
    this.cargarIngresosMetodoPago();
    this.cargarProductosMasVendidos();
    this.consultarVentasPorFecha();
  }


  obtenerFechaLocal(): string {

    const hoy = new Date();

    const anio = hoy.getFullYear();
    const mes = String(hoy.getMonth() + 1).padStart(2, '0');
    const dia = String(hoy.getDate()).padStart(2, '0');

    return `${anio}-${mes}-${dia}`;
  }


  cargarVentasHoy(): void {

    this.http.get<any>(
      `${this.apiUrl}/ventas-hoy`
    ).subscribe({

      next: (respuesta) => {
        this.ventasHoy = respuesta;
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
      this.mensaje = 'Selecciona una fecha.';
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
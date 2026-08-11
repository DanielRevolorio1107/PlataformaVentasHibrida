import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-productos',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './productos.component.html',
  styleUrl: './productos.component.scss'
})
export class ProductosComponent implements OnInit {
  mostrarFormulario = false;
  productoEditando: any = null;
  usuario: any = null;
  nuevoProducto = {
  nombre: '',
  descripcion: '',
  precio: 0
  };
  productos: any[] = [];

  mensaje = '';
  cargando = false;

  private apiUrl = 'http://localhost:5080/api/productos';

  constructor(private http: HttpClient) {}

  ngOnInit(): void {

  const usuarioGuardado = localStorage.getItem('usuario');

  if (usuarioGuardado) {
    this.usuario = JSON.parse(usuarioGuardado);
  }

  this.cargarProductos();
}


  obtenerHeaders(): HttpHeaders {
    const token = localStorage.getItem('token');

    return new HttpHeaders({
      Authorization: `Bearer ${token}`
    });
  }

  cargarProductos(): void {

    this.http.get<any[]>(
      this.apiUrl,
      { headers: this.obtenerHeaders() }
    ).subscribe({

      next: (productos) => {
        this.productos = productos;
      },

      error: () => {
        this.mensaje = 'No se pudieron cargar los productos.';
      }

    });
  }

  crearProducto(): void {

  if (!this.nuevoProducto.nombre.trim()) {
    this.mensaje = 'El nombre es obligatorio.';
    return;
  }

  if (this.nuevoProducto.precio <= 0) {
    this.mensaje = 'El precio debe ser mayor que cero.';
    return;
  }

  this.cargando = true;
  this.mensaje = '';

  this.http.post<any>(
    this.apiUrl,
    this.nuevoProducto,
    { headers: this.obtenerHeaders() }
  ).subscribe({

    next: () => {
      this.mensaje = 'Producto creado correctamente.';
      this.cargando = false;
      this.mostrarFormulario = false;

      this.nuevoProducto = {
        nombre: '',
        descripcion: '',
        precio: 0
      };

      this.cargarProductos();
    },

    error: (error) => {
      this.cargando = false;

      if (error.status === 403) {
        this.mensaje = 'No tiene permisos para crear productos.';
        return;
      }

      this.mensaje = 'No se pudo crear el producto.';
    }
  });
}

editarProducto(producto: any): void {
  this.productoEditando = { ...producto };
  this.mensaje = '';
}

cancelarEdicion(): void {
  this.productoEditando = null;
}

guardarEdicion(): void {

  if (!this.productoEditando.nombre.trim()) {
    this.mensaje = 'El nombre es obligatorio.';
    return;
  }

  if (this.productoEditando.precio <= 0) {
    this.mensaje = 'El precio debe ser mayor que cero.';
    return;
  }

  this.cargando = true;
  this.mensaje = '';

  this.http.put(
    `${this.apiUrl}/${this.productoEditando.id}`,
    {
      nombre: this.productoEditando.nombre,
      descripcion: this.productoEditando.descripcion,
      precio: this.productoEditando.precio,
      activo: this.productoEditando.activo
    },
    { headers: this.obtenerHeaders() }
  ).subscribe({

    next: () => {
      this.mensaje = 'Producto actualizado correctamente.';
      this.cargando = false;
      this.productoEditando = null;
      this.cargarProductos();
    },

    error: () => {
      this.cargando = false;
      this.mensaje = 'No se pudo actualizar el producto.';
    }
  });
}

desactivarProducto(producto: any): void {

  if (!confirm(`¿Deseas desactivar "${producto.nombre}"?`)) {
    return;
  }

  this.http.patch(
    `${this.apiUrl}/${producto.id}/desactivar`,
    {},
    { headers: this.obtenerHeaders() }
  ).subscribe({

    next: () => {
      this.mensaje = 'Producto desactivado correctamente.';
      this.cargarProductos();
    },

    error: () => {
      this.mensaje = 'No se pudo desactivar el producto.';
    }
  });
}

activarProducto(producto: any): void {

  this.http.patch(
    `${this.apiUrl}/${producto.id}/activar`,
    {},
    { headers: this.obtenerHeaders() }
  ).subscribe({

    next: () => {
      this.mensaje = 'Producto activado correctamente.';
      this.cargarProductos();
    },

    error: () => {
      this.mensaje = 'No se pudo activar el producto.';
    }
  });
}

eliminarProducto(producto: any): void {

  if (!confirm(`¿Eliminar definitivamente "${producto.nombre}"?`)) {
    return;
  }

  this.http.delete(
    `${this.apiUrl}/${producto.id}`,
    { headers: this.obtenerHeaders() }
  ).subscribe({

    next: () => {
      this.mensaje = 'Producto eliminado correctamente.';
      this.cargarProductos();
    },

    error: (error) => {

      if (error.status === 409) {
        this.mensaje =
          'Este producto tiene ventas registradas y no puede eliminarse. Puedes desactivarlo.';
        return;
      }

      this.mensaje = 'No se pudo eliminar el producto.';
    }
  });
}
}
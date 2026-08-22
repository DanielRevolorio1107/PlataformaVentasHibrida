import { inject } from '@angular/core';
import {
  HttpErrorResponse,
  HttpInterceptorFn
} from '@angular/common/http';

import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

export const authInterceptor: HttpInterceptorFn = (req, next) => {

  const router = inject(Router);

  const token = localStorage.getItem('token');

  let request = req;

  if (token) {

    request = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  return next(request).pipe(

    catchError((error: HttpErrorResponse) => {

      if (
        error.status === 401 &&
        !req.url.includes('/api/usuarios/login')
      ) {

        localStorage.removeItem('token');
        localStorage.removeItem('usuario');

        router.navigate(['/login']);
      }

      return throwError(() => error);
    })

  );
};
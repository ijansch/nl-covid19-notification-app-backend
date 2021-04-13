// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { AppConfigService} from './app-config.service';


@Injectable({
  providedIn: 'root'
})
export class LabConfirmService {
  constructor(private readonly http: HttpClient,
    private readonly appConfigService: AppConfigService) {
    }

  private data: { GGDKey: string; DateOfSymptomsOnset: string; };

  private static errorHandler(error: HttpErrorResponse, caught: Observable<any>): Observable<any> {
    // TODO error handling
    throw error;
  }

  confirmLabId(labConfirmationIds: Array<string>, dateOfSymptomsOnset: string): Observable<any> {
    const serviceUrl = location.origin + '/pubtek';
    this.data = {
      'GGDKey': labConfirmationIds.join(''),
      'DateOfSymptomsOnset': dateOfSymptomsOnset
    };
    const headers = {
    };

    return this.http.put(serviceUrl, this.data, headers).pipe(catchError(LabConfirmService.errorHandler));
  }
}

﻿import { Injectable } from '@angular/core';
import { Response, Headers, RequestOptions } from '@angular/http';
import { HttpClient } from '@angular/common/http';
import { UserManager, User, Log } from 'oidc-client';
import { Observable } from 'rxjs/Observable';
import 'rxjs/add/operator/catch';
import 'rxjs/add/operator/map';
import { log } from 'util';
import { environment } from '../../../environments/environment';

@Injectable()
export class AuthService {
  userManager: UserManager;
  identityClientSettings: any;
  clientId: string;
  authority: string;

  constructor(private httpClient: HttpClient) {
    this.clientId = 'fabric-accesscontrol';
    this.authority = environment.fabricIdentityApiUri;

    const clientSettings: any = {
      authority: this.authority,
      client_id: this.clientId,
      redirect_uri: 'http://localhost:4200/oidc-callback.html',
      post_logout_redirect_uri: 'http://localhost:4200',
      response_type: 'id_token token',
      scope: [
        'openid',
        'profile',
        'fabric.profile',
        'fabric/authorization.read',
        'fabric/authorization.write',
        'fabric/idprovider.searchusers',
        'fabric/authorization.dos.write'
      ].join(' '),
      silent_redirect_uri: 'http://localhost:4200/silent.html',
      automaticSilentRenew: true,
      filterProtocolClaims: true,
      loadUserInfo: true
    };
    this.userManager = new UserManager(clientSettings);

    this.userManager.events.addAccessTokenExpiring(function() {
      console.log('access token expiring');
    });

    this.userManager.events.addSilentRenewError(function(e) {
      console.log('silent renew error: ' + e.message);
    });

    this.userManager.events.addAccessTokenExpired(() => {
      console.log('access token expired');
      // when access token expires logout the user
      this.logout();
    });
  }

  login() {
    return this.userManager
      .signinRedirect();
  }

  logout() {
    this.userManager.signoutRedirect();
  }

  handleSigninRedirectCallback() {
    this.userManager
      .signinRedirectCallback()
      .then(user => {
        if (user) {
          console.log('Logged in: ' + JSON.stringify(user.profile));
        } else {
          console.log('could not log user in');
        }
      })
      .catch(e => {
        console.error(e);
      });
  }

  getUser(): Promise<User> {
    return this.userManager.getUser();
  }

  isUserAuthenticated() {
    return this.userManager.getUser().then(function(user) {
      if (user) {
        console.log('signin redirect done. ');
        console.log(user.profile);
        return true;
      } else {
        console.log('User is not logged in');
        return false;
      }
    });
  }

  private getAccessToken(): Promise<string> {
    return this.getUser().then(function(user) {
      if (user) {
        return Promise.resolve(user.access_token);
      }
    });
  }

  private handleError(error: Response | any) {
    Log.error('Error Response:');
    Log.error(error.message || error);
    return Observable.throw(error.message || error);
  }

  get<T>(resource: string): Promise<T> {
    return this.getAccessToken().then(token => {
      const requestUrl = this.authority + '/' + resource;
      return this.httpClient
        .get(requestUrl)
        .map((res: Response) => {
          return res.json();
        })
        .catch(error => this.handleError(error))
        .toPromise<T>();
    });
  }
}

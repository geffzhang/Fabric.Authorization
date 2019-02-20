import { map, mergeMap } from 'rxjs/operators';
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';

import { ConfigService } from './config.service';
import { OData } from './odata';

export interface IService {
    name: string;
    version?: number;
    url?: string;
    requireAuthToken: boolean;
}

interface IDiscoveryService {
    ServiceUrl: string;
    ServiceName: string;
    Version: number;
}

@Injectable()
export class ServicesService {
    public services: IService[] = [
        {
            name: 'IdentityService',
            requireAuthToken: false
        },
        {
            name: 'AuthorizationService',
            requireAuthToken: true
        },
        {
            name: 'IdentityProviderSearchService',
            requireAuthToken: true
        },
        {
            name: 'AccessControl',
            requireAuthToken: false
        },
        {
            name: 'DiscoveryService',
            requireAuthToken: true
        }
    ];

    constructor(private http: HttpClient, private configService: ConfigService) {}

    public initialize(): Observable<string> {
        var observable: Observable<string> = null;
        if(this.configService.getIdentityServiceRoot()) {
            observable = of(this.discoveryServiceEndpoint)
        } else {
            observable = this.configService.getDiscoveryServiceRoot()
        }

        return observable.pipe(
            mergeMap(discoveryServiceRoot => {
                const url: string =
                    `${discoveryServiceRoot}/Services?$filter=` +
                    this.services.map(service => `ServiceName eq \'${service.name}\'`).join(' or ') +
                    `&$select=ServiceUrl,Version,ServiceName`;
                return this.http.get<OData.IArray<IDiscoveryService>>(url, {withCredentials: true});
            }),
            map(response => {
                for (const service of this.services) {
                    const targetService: IDiscoveryService = response.value.find(
                        s => s.ServiceName === service.name && (!service.version || s.Version === service.version)
                    );

                    if (service.name === 'DiscoveryService') {
                        service.url = this.discoveryServiceEndpoint;
                    } else if (targetService === undefined) {
                        throw new Error(`The ${service.name} was not found in discovery service. Please ensure it is set up correctly.`);
                    } else {
                        service.url = targetService.ServiceUrl;
                    }
                    
                    return this.services.find(s => s.name === 'IdentityService').url;
                }
            })
        );
    }

    private baseDiscoveryServiceUrl : string;
    get discoveryServiceEndpoint(): string {
        if(this.baseDiscoveryServiceUrl === undefined) {
            const identityRootUrl: string = this.configService.getIdentityServiceRoot();
            const url: string = `${identityRootUrl}/.well-known/openid-configuration`;
            this.baseDiscoveryServiceUrl = this.http.get(url).discovery_uri;
        }

        return this.baseDiscoveryServiceUrl;
    }

    get identityServiceEndpoint(): string {
        return this.services.find(s => s.name === 'IdentityService').url;
    }

    get authorizationServiceEndpoint(): string {
        return this.services.find(s => s.name === 'AuthorizationService').url;
    }

    get identityProviderSearchServiceEndpoint(): string {
        return this.services.find(s => s.name === 'IdentityProviderSearchService').url;
    }

    get accessControlEndpoint(): string {
        const url = this.configService.getAccessControlServiceRoot();
        if(url) {
            return url
        }

        return this.services.find(s => s.name === 'AccessControl').url;
    }

    public needsAuthToken(url: string) {
        const urlLowerCase = url.toLowerCase();
        const targetService: IService = this.services.find(s => urlLowerCase.includes(s.name.toLowerCase())) ? this.services.find(s => urlLowerCase.includes(s.name.toLowerCase())) : this.findUrlBestMatch(this.services, url);
        return targetService ? targetService.requireAuthToken : false;
    }

    public findUrlBestMatch(services: IService[], url: string)
    {
       var serviceUrlMatch = "";
       services.forEach(element => {
        var result = url.toLowerCase().includes(element.url.toLowerCase());
        if (result)
        {
          if (element.url.length > serviceUrlMatch.length)
          {
            serviceUrlMatch = element.url.toLowerCase();
          }
        }
       });
       var matchedService = services.find(s => s.url.toLowerCase() === serviceUrlMatch);
       const serviceItem: IService = matchedService;
       return serviceItem;
    }
}

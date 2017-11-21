import { Component, OnInit } from '@angular/core';

declare var $: any;

//models
import { ClientAPI, APIParameter, ParameterType, PostDataValueType } from '../../../models/client-api.model';
import { SBOType } from '../../../models/log.model';

//service
import { ClientAPIService } from '../../../services/client-api.service';

@Component({
  selector: 'app-client-api',
  templateUrl: './client-api.component.html',
  styleUrls: ['./client-api.component.css']
})
export class ClientApiComponent implements OnInit {

  NewParameter: APIParameter = new APIParameter();
  Type: typeof SBOType = SBOType;
  PostDataType: typeof PostDataValueType = PostDataValueType;
  
  TransactionType: Array<string> = Object.keys(SBOType).filter(itm => !isNaN(Number.parseInt(itm)));
  
  Model: ClientAPI = new ClientAPI();
  APIs: ClientAPI[] = [];

  constructor(private apiService: ClientAPIService) { }

  ngOnInit() {
    console.log(Object.keys(SBOType));
    this.loadData();
  }

  async loadData(){
    this.APIs = await this.apiService.getAll();
  }

  onAddNewParam() {
    this.Model.Params.push(this.NewParameter);
    this.NewParameter = new APIParameter();
  }

  onDeleteParam(param: APIParameter){
    var index = this.Model.Params.indexOf(param, 0);
    if (index > -1) {
       this.Model.Params.splice(index, 1);
    }
  }

  onClear(){
    this.Model = new ClientAPI();
    this.NewParameter = new APIParameter();
  }

  async onSave(){
    let result = await this.apiService.saveAPI(this.Model);
    if (result.toLowerCase() != 'success') {
      $.notify({
        icon: "notifications",
        message: "<b>Client API</b> - " + result

      }, {
          type: 'danger',
          timer: 500,
          placement: {
            from: 'top',
            align: 'right'
          }
        });
    }
    else {
      this.Model = new ClientAPI();
      this.NewParameter = new APIParameter();
      this.loadData();
      $.notify({
        icon: "notifications",
        message: "<b>Success</b> - New api is saved."

      }, {
          type: 'success',
          timer: 500,
          placement: {
            from: 'top',
            align: 'right'
          }
        });
    }
  }

  async deleteApi(id: number){
    let result = await this.apiService.deleteAPI(id);
    if (result.toLowerCase() != 'success') {
      $.notify({
        icon: "notifications",
        message: "<b>Client API</b> - " + result

      }, {
          type: 'danger',
          timer: 500,
          placement: {
            from: 'top',
            align: 'right'
          }
        });
    }
    else {
      this.NewParameter = new APIParameter();
      this.loadData();
    }
  }
}

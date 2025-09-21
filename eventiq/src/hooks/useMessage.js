import React from 'react';
import { message } from 'antd';

export const useMessage = () => {
    const [messageApi, contextHolder] = message.useMessage();
    const success = (msg) => {
        messageApi.open({
            type: 'success',
            content: msg,
        });
    };
    const error = (msg) => {
        messageApi.open({
            type: 'error',
            content: msg,
        });
    
    };
    const warning = (msg) => {
        messageApi.open({
            type: 'warning',
            content: msg,
        });
    };
    const info = (msg) => {
        messageApi.open({
            type: 'info',
            content: msg,
        });
    }
    return {
       info, success, warning, error, contextHolder
    }
}
import React from 'react';
import { Modal, Button, Typography, message } from 'antd';
import { CreditCardOutlined, CopyOutlined } from '@ant-design/icons';

const { Text } = Typography;

const PaymentNoteModal = ({ visible, onCancel }) => {
  const copyToClipboard = (text, label) => {
    navigator.clipboard.writeText(text).then(() => {
      message.success(`${label} copied to clipboard!`);
    }).catch(() => {
      message.error('Failed to copy');
    });
  };

  const paymentTestInfo = {
    bank: 'NCB',
    cardNumber: '9704198526191432198',
    cardHolder: 'NGUYEN VAN A',
    expiryDate: '07/15',
    otp: '123456'
  };

  return (
    <Modal
      title={
        <div className="flex items-center gap-2">
          <CreditCardOutlined className="text-orange-500" />
          <span>Test Payment Card Information</span>
        </div>
      }
      open={visible}
      onCancel={onCancel}
      footer={[
        <Button key="close" onClick={onCancel}>
          Close
        </Button>
      ]}
      width={500}
    >
      <div className="space-y-4">
        <div className="bg-orange-50 border border-orange-200 rounded-lg p-4">
          <Text className="text-sm text-black-600 block mb-3  font-bold">
            Before testing, open new tab to get payment test account information below
          </Text>
          
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <div>
                <Text strong className="text-gray-700">Ngân hàng:</Text>
                <Text className="ml-2">{paymentTestInfo.bank}</Text>
              </div>
              <Button 
                type="text" 
                size="small" 
                icon={<CopyOutlined />}
                onClick={() => copyToClipboard(paymentTestInfo.bank, 'Bank')}
              />
            </div>

            <div className="flex items-center justify-between">
              <div>
                <Text strong className="text-gray-700">Số thẻ:</Text>
                <Text className="ml-2 font-mono">{paymentTestInfo.cardNumber}</Text>
              </div>
              <Button 
                type="text" 
                size="small" 
                icon={<CopyOutlined />}
                onClick={() => copyToClipboard(paymentTestInfo.cardNumber, 'Card Number')}
              />
            </div>

            <div className="flex items-center justify-between">
              <div>
                <Text strong className="text-gray-700">Tên chủ thẻ:</Text>
                <Text className="ml-2">{paymentTestInfo.cardHolder}</Text>
              </div>
              <Button 
                type="text" 
                size="small" 
                icon={<CopyOutlined />}
                onClick={() => copyToClipboard(paymentTestInfo.cardHolder, 'Card Holder')}
              />
            </div>

            <div className="flex items-center justify-between">
              <div>
                <Text strong className="text-gray-700">Ngày phát hành:</Text>
                <Text className="ml-2">{paymentTestInfo.expiryDate}</Text>
              </div>
              <Button 
                type="text" 
                size="small" 
                icon={<CopyOutlined />}
                onClick={() => copyToClipboard(paymentTestInfo.expiryDate, 'Expiry Date')}
              />
            </div>

            <div className="flex items-center justify-between">
              <div>
                <Text strong className="text-gray-700">Mật khẩu OTP:</Text>
                <Text className="ml-2 font-mono">{paymentTestInfo.otp}</Text>
              </div>
              <Button 
                type="text" 
                size="small" 
                icon={<CopyOutlined />}
                onClick={() => copyToClipboard(paymentTestInfo.otp, 'OTP')}
              />
            </div>
          </div>
        </div>

        <div className="bg-blue-50 border border-blue-200 rounded-lg p-3">
          <Text className="text-xs text-gray-600">
            💡 Tip: Click the copy icon next to each field to quickly copy the information.
          </Text>
        </div>
      </div>
    </Modal>
  );
};

export default PaymentNoteModal;

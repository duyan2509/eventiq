import React, { useState } from 'react';
import { Modal, Form, Input, Button } from 'antd';

const TransferTicketForm = ({ visible, onCancel, onConfirm, loading }) => {
  const [form] = Form.useForm();

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      onConfirm(values.toUserEmail, values.password);
      form.resetFields();
    } catch (error) {
      console.error('Validation failed:', error);
    }
  };

  const handleCancel = () => {
    form.resetFields();
    onCancel();
  };

  return (
    <Modal
      title="Transfer Ticket"
      open={visible}
      onCancel={handleCancel}
      footer={[
        <Button key="cancel" onClick={handleCancel}>
          Cancel
        </Button>,
        <Button key="submit" type="primary" loading={loading} onClick={handleSubmit}>
          Confirm Transfer
        </Button>,
      ]}
    >
      <Form form={form} layout="vertical">
        <Form.Item
          name="toUserEmail"
          label="Receiver Email"
          rules={[
            { required: true, message: 'Please enter receiver email' },
            { type: 'email', message: 'Please enter a valid email' },
          ]}
        >
          <Input placeholder="Enter receiver email address" />
        </Form.Item>
        <Form.Item
          name="password"
          label="Your Password"
          rules={[{ required: true, message: 'Please enter your password' }]}
        >
          <Input.Password placeholder="Enter your password to confirm" />
        </Form.Item>
      </Form>
    </Modal>
  );
};

export default TransferTicketForm;

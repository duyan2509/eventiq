import React, { useState } from 'react';
import { Modal, Form, Input, Upload, Button, Image, Space } from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import { payoutAPI } from '../../services/api';
import { useMessage } from '../../hooks/useMessage';

const { TextArea } = Input;

const PayoutModal = ({ visible, payout, onSuccess, onCancel }) => {
    const [form] = Form.useForm();
    const [loading, setLoading] = useState(false);
    const [proofImageFile, setProofImageFile] = useState(null);
    const [proofImagePreview, setProofImagePreview] = useState(payout?.proofImageUrl || null);
    const { success, error, contextHolder } = useMessage();

    const handleSubmit = async (values) => {
        try {
            setLoading(true);
            await payoutAPI.updatePayout(payout.id, {
                proofImage: proofImageFile,
                notes: values.notes,
            });
            success('Payout marked as paid successfully');
            form.resetFields();
            setProofImageFile(null);
            setProofImagePreview(null);
            onSuccess();
        } catch (err) {
            error(err?.response?.data?.message || 'Failed to update payout');
        } finally {
            setLoading(false);
        }
    };

    const handleImageChange = (info) => {
        const file = info.fileList.length > 0 ? info.fileList[info.fileList.length - 1].originFileObj : null;
        if (file) {
            setProofImageFile(file);
            setProofImagePreview(URL.createObjectURL(file));
        } else {
            setProofImageFile(null);
            setProofImagePreview(null);
        }
    };

    const handleImageRemove = () => {
        setProofImageFile(null);
        setProofImagePreview(null);
    };

    const formatPrice = (price) => {
        return new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND',
        }).format(price);
    };

    return (
        <>
            {contextHolder}
            <Modal
                title="Mark Payout as Paid"
                open={visible}
                onCancel={onCancel}
                footer={null}
                width={600}
            >
                <Form
                    form={form}
                    layout="vertical"
                    onFinish={handleSubmit}
                    initialValues={{
                        notes: payout?.notes || '',
                    }}
                >
                    <div style={{ marginBottom: 16 }}>
                        <p><strong>Event:</strong> {payout?.eventName}</p>
                        <p><strong>Event Item:</strong> {payout?.eventItemName}</p>
                        <p><strong>Organization:</strong> {payout?.organizationName}</p>
                        <p><strong>Platform Fee (20%):</strong> {formatPrice(payout?.platformFee || 0)}</p>
                        <p><strong>Org Amount (80%):</strong> {formatPrice(payout?.orgAmount || 0)}</p>
                    </div>

                    <Form.Item
                        label="Payment Proof Image"
                        name="proofImage"
                    >
                        <Upload
                            listType="picture-card"
                            showUploadList={false}
                            beforeUpload={() => false}
                            onChange={handleImageChange}
                            onRemove={handleImageRemove}
                            accept="image/*"
                            maxCount={1}
                        >
                            {!proofImagePreview ? (
                                <div>
                                    <PlusOutlined />
                                    <div style={{ marginTop: 8 }}>Upload</div>
                                </div>
                            ) : (
                                <Image
                                    src={proofImagePreview}
                                    alt="proof preview"
                                    style={{ width: '100%', height: '100%', objectFit: 'cover' }}
                                    preview={false}
                                />
                            )}
                        </Upload>
                    </Form.Item>

                    <Form.Item
                        label="Notes"
                        name="notes"
                    >
                        <TextArea
                            rows={4}
                            placeholder="Enter notes about this payout"
                        />
                    </Form.Item>

                    <Form.Item>
                        <Space>
                            <Button type="primary" htmlType="submit" loading={loading}>
                                Mark as Paid
                            </Button>
                            <Button onClick={onCancel}>Cancel</Button>
                        </Space>
                    </Form.Item>
                </Form>
            </Modal>
        </>
    );
};

export default PayoutModal;


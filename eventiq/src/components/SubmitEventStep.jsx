import React, { useState } from 'react';
import { Button, Card, List, Tag, Modal, Table, Typography, Space } from 'antd';
import { CheckCircleOutlined, CloseCircleOutlined, HistoryOutlined } from '@ant-design/icons';
import { eventAPI } from '../services/api';
import { useMessage } from '../hooks/useMessage';

const { Title, Text } = Typography;

const SubmitEventStep = ({ eventId, onValidationChange }) => {
    const { error, success, contextHolder } = useMessage();
    const [validationResult, setValidationResult] = useState(null);
    const [validating, setValidating] = useState(false);
    const [submitting, setSubmitting] = useState(false);
    const [approvalHistoryVisible, setApprovalHistoryVisible] = useState(false);
    const [approvalHistory, setApprovalHistory] = useState([]);
    const [loadingHistory, setLoadingHistory] = useState(false);

    const handleValidate = async () => {
        setValidating(true);
        try {
            const result = await eventAPI.validateEvent(eventId);
            setValidationResult(result);
            if (result.isValid) {
                success('Event validation passed! You can now submit the event.');
            } else {
                error(`Validation failed: ${result.errors.join(', ')}`);
            }
        } catch (err) {
            error(err?.response?.data?.message || 'Failed to validate event');
        } finally {
            setValidating(false);
        }
    };

    const handleSubmit = async () => {
        if (!validationResult || !validationResult.isValid) {
            error('Please validate the event first');
            return;
        }

        setSubmitting(true);
        try {
            const result = await eventAPI.submitEvent(eventId);
            success(result.message || 'Event submitted successfully for approval');
            // Reset validation result
            setValidationResult(null);
        } catch (err) {
            error(err?.response?.data?.message || 'Failed to submit event');
        } finally {
            setSubmitting(false);
        }
    };

    const handleViewApprovalHistory = async () => {
        setApprovalHistoryVisible(true);
        setLoadingHistory(true);
        try {
            const history = await eventAPI.getApprovalHistory(eventId);
            setApprovalHistory(history);
        } catch (err) {
            error('Failed to load approval history');
        } finally {
            setLoadingHistory(false);
        }
    };

    const getStatusTag = (status) => {
        const colors = {
            'Draft': 'default',
            'Pending': 'processing',
            'Published': 'success',
        };
        return <Tag color={colors[status] || 'default'}>{status}</Tag>;
    };

    const historyColumns = [
        {
            title: 'Date',
            dataIndex: 'actionDate',
            key: 'actionDate',
            render: (date) => new Date(date).toLocaleString(),
        },
        {
            title: 'From',
            dataIndex: 'previousStatus',
            key: 'previousStatus',
            render: (status) => getStatusTag(status),
        },
        {
            title: 'To',
            dataIndex: 'newStatus',
            key: 'newStatus',
            render: (status) => getStatusTag(status),
        },
        {
            title: 'By',
            dataIndex: 'approvedByUserName',
            key: 'approvedByUserName',
        },
        {
            title: 'Comment',
            dataIndex: 'comment',
            key: 'comment',
        },
    ];

    return (
        <div className="space-y-4">
            {contextHolder}
            <Space direction="vertical" size="large" style={{ width: '100%' }}>
                <div>
                    <Title level={4}>Validation Status</Title>
                    {validationResult && (
                        <Card>
                            {validationResult.isValid ? (
                                <div className="text-green-600">
                                    <CheckCircleOutlined /> All validations passed!
                                </div>
                            ) : (
                                <div>
                                    <div className="text-red-600 mb-2">
                                        <CloseCircleOutlined /> Validation failed
                                    </div>
                                    {validationResult.errors.length > 0 && (
                                        <List
                                            size="small"
                                            bordered
                                            dataSource={validationResult.errors}
                                            renderItem={(item) => (
                                                <List.Item className="text-red-600">
                                                    {item}
                                                </List.Item>
                                            )}
                                        />
                                    )}
                                    {validationResult.warnings.length > 0 && (
                                        <div className="mt-2">
                                            <Text type="warning">Warnings:</Text>
                                            <List
                                                size="small"
                                                dataSource={validationResult.warnings}
                                                renderItem={(item) => (
                                                    <List.Item>
                                                        <Text type="warning">{item}</Text>
                                                    </List.Item>
                                                )}
                                            />
                                        </div>
                                    )}
                                </div>
                            )}
                        </Card>
                    )}
                </div>

                <Space>
                    <Button
                        type="default"
                        onClick={handleValidate}
                        loading={validating}
                    >
                        Validate Event
                    </Button>
                    <Button
                        type="primary"
                        onClick={handleSubmit}
                        loading={submitting}
                        disabled={!validationResult || !validationResult.isValid}
                    >
                        Submit for Approval
                    </Button>
                    <Button
                        icon={<HistoryOutlined />}
                        onClick={handleViewApprovalHistory}
                    >
                        View Approval History
                    </Button>
                </Space>
            </Space>

            <Modal
                title="Approval History"
                open={approvalHistoryVisible}
                onCancel={() => setApprovalHistoryVisible(false)}
                footer={null}
                width={800}
            >
                <Table
                    dataSource={approvalHistory}
                    columns={historyColumns}
                    rowKey="id"
                    loading={loadingHistory}
                    pagination={false}
                />
            </Modal>
        </div>
    );
};

export default SubmitEventStep;


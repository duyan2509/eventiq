import React, { useEffect, useState } from 'react';
import { Table, Button, Modal, Form, Input, Popconfirm } from 'antd';
import { eventAPI } from '../services/api';
import { useMessage } from '../hooks/useMessage';
const SeatMapManager = ({ eventId, orgId }) => {
    const { error, success, contextHolder } = useMessage();
    const [charts, setCharts] = useState([]);
    const [isModalVisible, setIsModalVisible] = useState(false);
    const [isEditMode, setIsEditMode] = useState(false);
    const [selectedChart, setSelectedChart] = useState(null);
    const [form] = Form.useForm();
    const [loading, setLoading] = useState(false);

    const fetchCharts = async () => {
        try {
            const response = await eventAPI.getEventCharts(eventId);
            setCharts(response);
        } catch (err) {
            error('Failed to fetch seat maps');
        }
    };

    useEffect(() => {
        fetchCharts();
    }, [eventId]);

    const handleSubmit = async (values) => {
        setLoading(true);
        try {
            if (isEditMode) {
                await eventAPI.updateEventChart(eventId, selectedChart.id, values);
                success('Seat map updated successfully');
            } else {
                await eventAPI.addEventChart(eventId, values);
                success('Seat map added successfully');
            }
            setIsModalVisible(false);
            setIsEditMode(false);
            setSelectedChart(null);
            form.resetFields();
            fetchCharts();
        } catch (err) {
            error(`Failed to ${isEditMode ? 'update' : 'add'} seat map`);
        } finally {
            setLoading(false);
        }
    };

    const handleDelete = async (chartId) => {
        try {
            await eventAPI.deleteEventChart(eventId, chartId);
            message.success('Seat map deleted successfully');
            fetchCharts();
        } catch (err) {
            message.error('Failed to delete seat map');
        }
    };

    const columns = [
        {
            title: 'Name',
            dataIndex: 'name',
            key: 'name',
        },
        {
            title: 'Action',
            key: 'action',
            render: (_, record) => (
                <div className="flex gap-2">
                    <Button
                        onClick={() => {
                            setIsEditMode(true);
                            setSelectedChart(record);
                            setIsModalVisible(true);
                            form.setFieldsValue({ name: record.name });
                        }}
                    >
                        Edit
                    </Button>
                    <Popconfirm
                        title="Delete seat map"
                        description="Are you sure to delete this seat map?"
                        onConfirm={() => handleDelete(record.id)}
                        okText="Yes"
                        cancelText="No"
                    >
                        <Button danger>Delete</Button>
                    </Popconfirm>
                    <Button
                        type="primary"
                        onClick={() => {
                            window.open(`/org/${orgId}/event/${eventId}/seat-map/${record.id}`, '_blank');
                        }}
                    >
                        Seat Map Setup
                    </Button>
                </div>
            ),
        },
    ];

    return (
        <div className="space-y-4">
            {contextHolder}
            <div className="flex justify-between items-center">
                <h2 className="text-xl font-semibold">Seat Maps</h2>
                <Button type="primary" onClick={() => setIsModalVisible(true)}>
                    Add Seat Map
                </Button>
            </div>

            <Table
                dataSource={charts}
                columns={columns}
                rowKey="id"
            />

            <Modal
                title={isEditMode ? "Edit Seat Map" : "Add New Seat Map"}
                open={isModalVisible}
                onCancel={() => {
                    setIsModalVisible(false);
                    setIsEditMode(false);
                    setSelectedChart(null);
                    form.resetFields();
                }}
                footer={null}
            >
                <Form
                    form={form}
                    onFinish={handleSubmit}
                    layout="vertical"
                >
                    <Form.Item
                        name="name"
                        label="Seat Map Name"
                        rules={[
                            { required: true, message: 'Please input seat map name!' }
                        ]}
                    >
                        <Input />
                    </Form.Item>

                    <div className="flex justify-end gap-2">
                        <Button onClick={() => {
                            setIsModalVisible(false);
                            form.resetFields();
                        }}>
                            Cancel
                        </Button>
                        <Button type="primary" htmlType="submit" loading={loading}>
                            {isEditMode ? 'Update' : 'Create'}
                        </Button>
                    </div>
                </Form>
            </Modal>
        </div>
    );
};

export default SeatMapManager;
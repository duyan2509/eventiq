import React from 'react';
import { Card, Row, Col, Statistic, Tag, Button, Space, Image, Modal } from 'antd';
import { IdcardOutlined, DollarOutlined, CheckCircleOutlined, EyeOutlined } from '@ant-design/icons';

const OrgRevenueStats = ({ stats, loading }) => {
    const [proofModalVisible, setProofModalVisible] = React.useState(false);

    const formatPrice = (price) => {
        return new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND',
        }).format(price);
    };

    return (
        <Card title="Revenue Statistics" style={{ marginBottom: 24 }}>
            <Row gutter={16}>
                <Col xs={24} sm={12} lg={6}>
                    <Card>
                        <Statistic
                            title="Total Sold Tickets"
                            value={stats?.totalSoldTickets || 0}
                            prefix={<IdcardOutlined />}
                            loading={loading}
                        />
                    </Card>
                </Col>
                <Col xs={24} sm={12} lg={6}>
                    <Card>

                        <Statistic
                            title="Total Gross Revenue"
                            value={stats?.totalGrossRevenue || 0}
                            prefix={<DollarOutlined />}
                            formatter={(value) => formatPrice(value)}
                            loading={loading}
                        />
                    </Card>
                </Col>
                <Col xs={24} sm={12} lg={6}>
                    <Card>
                        <Statistic
                            title="Total Organization Amount (80%)"
                            value={stats?.totalOrganizationAmount || 0}
                            prefix={<DollarOutlined />}

                            formatter={(value) => formatPrice(value)}
                            valueStyle={{ color: '#3f8600' }}
                            loading={loading}
                        />
                    </Card>
                </Col>
                <Col xs={24} sm={12} lg={6}>
                    <Card>
                        <div style={{ marginBottom: 8 }}>
                            <strong>Payout Status:</strong>
                        </div>
                        <Space>
                            <Tag color={stats?.payoutStatus === 'PAID' ? 'green' : 'orange'}>
                                {stats?.payoutStatus === 'PAID' ? (
                                    <span>
                                        <CheckCircleOutlined /> Paid
                                    </span>
                                ) : (
                                    'Pending'
                                )}
                            </Tag>
                            {stats?.payoutStatus === 'PAID' && stats?.proofImageUrl && (
                                <Button
                                    type="link"
                                    size="small"
                                    icon={<EyeOutlined />}
                                    onClick={() => setProofModalVisible(true)}
                                >
                                    View Proof
                                </Button>
                            )}
                        </Space>
                        {stats?.payoutDate && (
                            <div style={{ marginTop: 8, fontSize: 12, color: '#666' }}>
                                Payout Date: {new Date(stats.payoutDate).toLocaleDateString('en-US', {
                                    year: 'numeric',
                                    month: '2-digit',
                                    day: '2-digit',
                                    hour: '2-digit',
                                    minute: '2-digit'
                                })}
                            </div>
                        )}
                    </Card>
                </Col>
            </Row>

            <Modal
                title="Payout Proof"
                open={proofModalVisible}
                onCancel={() => setProofModalVisible(false)}
                footer={null}
                width={600}
            >
                {stats?.proofImageUrl && (
                    <Image
                        src={stats.proofImageUrl}
                        alt="Proof"
                        style={{ width: '100%' }}
                    />
                )}
            </Modal>
        </Card>
    );
};

export default OrgRevenueStats;

